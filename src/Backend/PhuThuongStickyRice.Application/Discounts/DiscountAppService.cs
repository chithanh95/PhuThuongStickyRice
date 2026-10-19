using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Discounts
{
    public interface IDiscountAppService : IApplicationService
    {
        Task<List<DiscountCodeDto>> GetListAsync();

        Task<DiscountCodeDto> GetAsync(Guid id);

        Task<DiscountCodeDto> CreateAsync(CreateDiscountCodeDto dto);

        Task<DiscountCodeDto> UpdateAsync(Guid id, CreateDiscountCodeDto dto);

        Task DeleteAsync(Guid id);

        /// <summary>Public: checks a code against a cart subtotal.</summary>
        Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input);

        /// <summary>
        /// Item 139 (Public): active, unexpired public discount codes that
        /// still have uses left. Used by checkout to auto-apply the best one.
        /// </summary>
        Task<List<DiscountCodeDto>> GetActivePublicAsync();

        /// <summary>
        /// perf-hunt 2026-10-19 (Public): returns the best applicable
        /// money-off public code for the subtotal in ONE call — replaces the
        /// old client loop of GetActivePublicAsync + N x ValidateAsync
        /// (N HTTP round-trips + N DB lookups on the checkout hot path).
        /// Freeship codes are skipped (separate flow); ties keep the newest
        /// code, exactly like the old client loop did.
        /// </summary>
        Task<BestDiscountResultDto> EvaluateBestAsync(EvaluateBestDiscountInput input);

        /// <summary>
        /// True when the code is a usable freeship voucher (item 120).
        /// Used by order creation to zero the delivery fee.
        /// </summary>
        Task<bool> IsFreeshipAsync(string code, decimal subtotal, string customerPhone);
        /// <summary>
        /// Order creation: returns the discount amount and bumps the usage
        /// counter. Throws DomainException when the code cannot be applied.
        /// </summary>
        Task<decimal> ApplyAsync(string code, decimal subtotal);

        /// <summary>
        /// Order creation with an owner phone (single-use spin/birthday
        /// codes are tied to a phone). Throws when the code cannot be applied.
        /// </summary>
        Task<decimal> ApplyAsync(string code, decimal subtotal, string customerPhone);

        /// <summary>
        /// Validates a code and returns the discount amount WITHOUT bumping
        /// the usage counter. Call <see cref="IncrementUsageAsync"/> after the
        /// order is successfully created.
        /// </summary>
        Task<decimal> ValidateAndComputeAsync(string code, decimal subtotal, string customerPhone);

        /// <summary>
        /// Gộp <see cref="ValidateAndComputeAsync"/> và <see cref="IsFreeshipAsync"/>
        /// thành 1 query duy nhất: validate mã, tính số tiền giảm và cho biết mã
        /// có phải freeship không. Dùng ở checkout để tránh SELECT cùng 1 row
        /// DiscountCode 2 lần cho mỗi đơn. Ném DomainException khi mã không dùng được.
        /// </summary>
        Task<(decimal DiscountValue, bool IsFreeship)> ValidateAndComputeWithFreeshipAsync(
            string code, decimal subtotal, string customerPhone);

        /// <summary>
        /// Bumps the usage counter for a code. Call after order creation succeeds.
        /// </summary>
        Task IncrementUsageAsync(string code);

        /// <summary>
        /// Public: checks a code against a cart subtotal for a given phone.
        /// </summary>
        Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input, string customerPhone);

        /// <summary>
        /// Creates a system-generated single-use code (lucky spin, birthday
        /// voucher). Internal — called from other application services.
        /// </summary>
        Task<DiscountCodeDto> CreateGeneratedAsync(CreateGeneratedDiscountDto dto);

        /// <summary>
        /// Internal: true when a usable code with the given prefix already
        /// exists for the phone (prevents duplicate spin/birthday codes).
        /// </summary>
        Task<bool> HasUsableCodeAsync(string codePrefix, string ownerPhone);

        /// <summary>
        /// Internal: subset of the given phones that already own a usable
        /// code with the given prefix — one batched query instead of calling
        /// <see cref="HasUsableCodeAsync"/> per phone (N+1 in birthday
        /// voucher loops). Filters are identical to
        /// <see cref="HasUsableCodeAsync"/>; membership of the returned set
        /// is equivalent to that method's result for each input phone.
        /// </summary>
        Task<HashSet<string>> GetPhonesWithUsableCodeAsync(string codePrefix, IEnumerable<string> phones);

        /// <summary>
        /// Item 143 (Internal): code of the first usable code with the given
        /// prefix for the phone, or null when none exists.
        /// </summary>
        Task<string> GetUsableCodeAsync(string codePrefix, string ownerPhone);
    }

    /// <summary>
    /// Quản lý mã giảm giá: admin tạo/sửa/xóa mã, kiểm tra và áp dụng mã vào
    /// đơn hàng (kể cả freeship), sinh mã dùng một lần cho vòng quay may mắn /
    /// sinh nhật, và đếm lượt sử dụng. Kết quả validate được cache ở shop nhưng
    /// lúc áp dụng luôn kiểm tra lại nên không bao giờ áp sai mã đã hết hạn.
    /// </summary>
    public class DiscountAppService : ApplicationService, IDiscountAppService
    {
        private readonly IDiscountCodeRepository _discounts;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;
        private readonly Products.IProductCatalogCache _catalogCache;

        public DiscountAppService(
            IDiscountCodeRepository discounts,
            IAsyncQueryableExecuter asyncExecuter,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger,
            Products.IProductCatalogCache catalogCache)
        {
            _discounts = discounts;
            _asyncExecuter = asyncExecuter;
            _permissions = permissions;
            _activityLogger = activityLogger;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Admin: 500 mã mới nhất (mã tự sinh theo khách như quay/sinh nhật nhiều,
        /// không lấy hết để không nặng trang).
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<DiscountCodeDto>> GetListAsync()
        {
            // perf-hunt 2026-10-19: DiscountCodes grows unbounded (lucky-spin /
            // birthday / reward-rule generate one code per customer). Cap the admin
            // list at the 500 newest like Wallet/Loyalty lists (Take(200)).
            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedDateTime).Take(500));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>Admin: chi tiết 1 mã theo Id.</summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<DiscountCodeDto> GetAsync(Guid id)
        {
            var entity = await _discounts.GetAsync(id);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin (cần quyền discounts.manage): tạo mã mới. Chuẩn hóa mã về in
        /// hoa, kiểm tra không trùng, xóa cache mã công khai để shop cập nhật ngay.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<DiscountCodeDto> CreateAsync(CreateDiscountCodeDto dto)
        {
            // Item 155: managing discount codes needs discounts.manage.
            await _permissions.RequirePermissionAsync(PermissionKeys.DiscountsManage);

            var code = NormalizeCode(dto.Code);
            await EnsureCodeUniqueAsync(code, null);
            EnsureValueProvided(dto);

            var entity = new DiscountCode
            {
                Code = code,
                DiscountPercent = dto.DiscountPercent,
                DiscountAmount = dto.DiscountAmount,
                MinOrderAmount = Math.Max(0, dto.MinOrderAmount),
                ExpiresAt = dto.ExpiresAt,
                MaxUses = dto.MaxUses,
                UsedCount = 0,
                IsActive = dto.IsActive,
                VoucherType = dto.VoucherType
            };
            await _discounts.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.DiscountCreate, $"Thêm mã giảm giá {entity.Code}.");

            return MapToDto(entity);
        }

        /// <summary>
        /// Admin (cần quyền discounts.manage): cập nhật mã. Kiểm tra trùng mã
        /// loại trừ chính nó (excludeId) để đổi các field khác không bị báo trùng.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<DiscountCodeDto> UpdateAsync(Guid id, CreateDiscountCodeDto dto)
        {
            // Item 155: managing discount codes needs discounts.manage.
            await _permissions.RequirePermissionAsync(PermissionKeys.DiscountsManage);

            var entity = await _discounts.GetAsync(id);
            var code = NormalizeCode(dto.Code);
            await EnsureCodeUniqueAsync(code, id);
            EnsureValueProvided(dto);

            entity.Code = code;
            entity.DiscountPercent = dto.DiscountPercent;
            entity.DiscountAmount = dto.DiscountAmount;
            entity.MinOrderAmount = Math.Max(0, dto.MinOrderAmount);
            entity.ExpiresAt = dto.ExpiresAt;
            entity.MaxUses = dto.MaxUses;
            entity.IsActive = dto.IsActive;
            entity.VoucherType = dto.VoucherType;
            await _discounts.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.DiscountUpdate, $"Sửa mã giảm giá {entity.Code}.");

            return MapToDto(entity);
        }

        /// <summary>Admin (cần quyền discounts.manage): xóa mã.</summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            // Item 155: managing discount codes needs discounts.manage.
            await _permissions.RequirePermissionAsync(PermissionKeys.DiscountsManage);

            var entity = await _discounts.GetAsync(id);
            await _discounts.DeleteAsync(entity, autoSave: true);
            _catalogCache.Invalidate();

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.DiscountDelete, $"Xóa mã giảm giá {entity.Code}.");
        }

        /// <summary>
        /// Public: kiểm tra mã với tổng tiền giỏ hàng, trả về kết quả và số
        /// tiền được giảm — CHƯA tăng lượt dùng.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input)
        {
            return await ValidateAsync(input, null);
        }

        /// <summary>
        /// Public: kiểm tra mã cho 1 SĐT cụ thể (mã riêng tư gắn SĐT như mã
        /// quay/sinh nhật chỉ chủ SĐT mới dùng được).
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input, string customerPhone)
        {
            var code = NormalizeCode(input.Code);
            var queryable = await _discounts.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == code));

            var (valid, message) = CheckUsable(entity, input.Subtotal, customerPhone);
            return new ValidateDiscountResultDto
            {
                Valid = valid,
                Message = valid && entity.VoucherType == VoucherType.Freeship
                    ? "Áp dụng miễn phí ship thành công."
                    : message,
                DiscountAmount = valid ? ComputeDiscount(entity, input.Subtotal) : 0,
                IsFreeship = valid && entity.VoucherType == VoucherType.Freeship
            };
        }

        /// <summary>
        /// True khi mã tồn tại, là loại freeship và còn dùng được với tổng
        /// tiền + SĐT cho trước. Checkout dùng để quyết định miễn phí ship.
        /// </summary>
        public virtual async Task<bool> IsFreeshipAsync(string code, decimal subtotal, string customerPhone)
        {
            var normalized = NormalizeCode(code);
            var queryable = await _discounts.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));

            if (entity == null || entity.VoucherType != VoucherType.Freeship)
            {
                return false;
            }

            var (valid, _) = CheckUsable(entity, subtotal, customerPhone);
            return valid;
        }

        /// <summary>
        /// Used by order creation: returns the discount amount and bumps the
        /// usage counter. Throws <see cref="Domain.Exceptions.DomainException"/>
        /// when the code cannot be applied.
        /// </summary>
        public virtual async Task<decimal> ApplyAsync(string code, decimal subtotal)
        {
            return await ApplyAsync(code, subtotal, null);
        }

        public virtual async Task<decimal> ApplyAsync(string code, decimal subtotal, string customerPhone)
        {
            // Validate trước rồi mới tăng lượt dùng: đơn lỗi không được đốt lượt mã.
            var amount = await ValidateAndComputeAsync(code, subtotal, customerPhone);
            await IncrementUsageAsync(code);
            return amount;
        }

        /// <summary>
        /// Kiểm tra mã và tính số tiền giảm nhưng KHÔNG tăng lượt dùng — dùng
        /// khi tính thử tổng tiền ở checkout trước lúc tạo đơn. Gọi
        /// <see cref="IncrementUsageAsync"/> sau khi tạo đơn thành công để đơn
        /// lỗi (thanh toán hỏng, khách bỏ giỏ) không đốt lượt mã.
        /// Ném <see cref="Domain.Exceptions.DomainException"/> khi mã không dùng được.
        /// </summary>
        public virtual async Task<decimal> ValidateAndComputeAsync(string code, decimal subtotal, string customerPhone)
        {
            var normalized = NormalizeCode(code);
            var queryable = await _discounts.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));

            var (valid, message) = CheckUsable(entity, subtotal, customerPhone);
            if (!valid)
            {
                throw new Domain.Exceptions.DomainException(message);
            }

            return ComputeDiscount(entity, subtotal);
        }

        /// <summary>
        /// Gộp validate + tính tiền giảm + xác định freeship trong 1 lần SELECT
        /// DiscountCode (perf-hunt 2026-10-19: checkout trước đây gọi
        /// ValidateAndComputeAsync rồi IsFreeshipAsync = 2 query cùng 1 row).
        /// Ngữ nghĩa giữ nguyên: validate ném DomainException khi mã không dùng
        /// được; IsFreeship chỉ true khi mã hợp lệ và là loại Freeship.
        /// </summary>
        public virtual async Task<(decimal DiscountValue, bool IsFreeship)> ValidateAndComputeWithFreeshipAsync(
            string code, decimal subtotal, string customerPhone)
        {
            var normalized = NormalizeCode(code);
            var queryable = await _discounts.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));

            var (valid, message) = CheckUsable(entity, subtotal, customerPhone);
            if (!valid)
            {
                throw new Domain.Exceptions.DomainException(message);
            }

            return (ComputeDiscount(entity, subtotal), entity.VoucherType == VoucherType.Freeship);
        }

        /// <summary>
        /// Tăng UsedCount lên 1 cho mã. Chỉ gọi sau khi tạo đơn thành công —
        /// không tự ý tăng ở bước tính thử, kẻo đơn lỗi vẫn đốt lượt mã.
        /// </summary>
        public virtual async Task IncrementUsageAsync(string code)
        {
            var normalized = NormalizeCode(code);
            var queryable = await _discounts.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));
            if (entity == null)
            {
                return;
            }

            // Tăng lượt dùng nguyên tử, có điều kiện MaxUses ngay trong câu
            // UPDATE: 2 checkout đồng thời cùng mã 1 lần dùng đều đã qua
            // validate thì chỉ bên UPDATE trước thành công; bên còn lại ném
            // lỗi để đơn (chung UoW) rollback thay vì dùng lậu lượt thứ 2.
            var incremented = await _discounts.TryIncrementUsageAsync(normalized);
            if (!incremented)
            {
                throw new Domain.Exceptions.DomainException(
                    "Mã giảm giá vừa được sử dụng hết. Vui lòng thử lại mà không dùng mã này.");
            }
        }

        /// <summary>
        /// Tạo mã dùng 1 lần do hệ thống sinh (vòng quay may mắn, quà sinh
        /// nhật): mã ngẫu nhiên theo tiền tố, MaxUses = 1, gắn SĐT chủ sở hữu
        /// để không ai dùng ké. Chỉ các service nội bộ gọi.
        /// </summary>
        public virtual async Task<DiscountCodeDto> CreateGeneratedAsync(CreateGeneratedDiscountDto dto)
        {
            if (!dto.DiscountPercent.HasValue && !dto.DiscountAmount.HasValue)
            {
                throw new Domain.Exceptions.DomainException(
                    "Phải nhập phần trăm giảm hoặc số tiền giảm.");
            }

            var code = await GenerateUniqueCodeAsync(
                string.IsNullOrWhiteSpace(dto.Prefix) ? "VOUCHER" : dto.Prefix.Trim().ToUpperInvariant());

            var entity = new DiscountCode
            {
                Code = code,
                DiscountPercent = dto.DiscountPercent,
                DiscountAmount = dto.DiscountAmount,
                MinOrderAmount = Math.Max(0, dto.MinOrderAmount),
                ExpiresAt = dto.ExpiresAt,
                MaxUses = 1,
                UsedCount = 0,
                IsActive = true,
                OwnerPhone = string.IsNullOrWhiteSpace(dto.OwnerPhone) ? null : dto.OwnerPhone.Trim()
            };
            await _discounts.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// True khi SĐT đã có mã còn dùng được với tiền tố cho trước — chống
        /// cấp trùng mã quay/sinh nhật cho cùng 1 khách.
        /// </summary>
        public virtual async Task<bool> HasUsableCodeAsync(string codePrefix, string ownerPhone)
        {
            if (string.IsNullOrWhiteSpace(codePrefix) || string.IsNullOrWhiteSpace(ownerPhone))
            {
                return false;
            }

            var prefix = codePrefix.Trim().ToUpperInvariant();
            var queryable = await _discounts.GetQueryableAsync();
            var existing = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x =>
                    x.Code.StartsWith(prefix)
                    && x.OwnerPhone == ownerPhone
                    && x.IsActive
                    && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > DateTime.UtcNow)
                    && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value)));
            return existing != null;
        }

        /// <summary>
        /// Batch version của <see cref="HasUsableCodeAsync"/>: trả về các SĐT
        /// trong danh sách đã có mã còn dùng được với tiền tố cho trước — 1
        /// query duy nhất (IN clause) thay vì 1 query/SĐT. Điều kiện lọc giữ
        /// nguyên như HasUsableCodeAsync nên kết quả tương đương từng phần tử.
        /// </summary>
        public virtual async Task<HashSet<string>> GetPhonesWithUsableCodeAsync(
            string codePrefix, IEnumerable<string> phones)
        {
            var result = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(codePrefix))
            {
                return result;
            }

            var list = (phones ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            if (list.Count == 0)
            {
                return result;
            }

            var prefix = codePrefix.Trim().ToUpperInvariant();
            var queryable = await _discounts.GetQueryableAsync();
            var rows = await _asyncExecuter.ToListAsync(
                queryable.Where(x =>
                    x.Code.StartsWith(prefix)
                    && list.Contains(x.OwnerPhone)
                    && x.IsActive
                    && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > DateTime.UtcNow)
                    && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value))
                    .Select(x => x.OwnerPhone));
            foreach (var phone in rows)
            {
                result.Add(phone);
            }

            return result;
        }

        /// <summary>
        /// Item 143: code of the first usable code with the given prefix for
        /// the phone, or null when none exists.
        /// </summary>
        public virtual async Task<string> GetUsableCodeAsync(string codePrefix, string ownerPhone)
        {
            if (string.IsNullOrWhiteSpace(codePrefix) || string.IsNullOrWhiteSpace(ownerPhone))
            {
                return null;
            }

            var prefix = codePrefix.Trim().ToUpperInvariant();
            var queryable = await _discounts.GetQueryableAsync();
            var existing = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x =>
                    x.Code.StartsWith(prefix)
                    && x.OwnerPhone == ownerPhone
                    && x.IsActive
                    && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > DateTime.UtcNow)
                    && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value)));
            return existing?.Code;
        }

        /// <summary>
        /// Item 139 (Public): active, unexpired public discount codes that
        /// still have uses left. Used by checkout to auto-apply the best one.
        /// perf-hunt 2026-10-19: called on every checkout/cart view, so cache
        /// 5 minutes via IProductCatalogCache. Safe because ApplyAsync /
        /// ValidateAndComputeAsync re-check IsActive/expiry/MaxUses server-side
        /// at apply time; worst case a just-exhausted code shows for ≤5 min and
        /// fails validation gracefully. Invalidated on every write path.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<List<DiscountCodeDto>> GetActivePublicAsync()
        {
            return await _catalogCache.GetOrAddAsync("discounts:active-public", async () =>
            {
                var now = DateTime.UtcNow;
                var queryable = await _discounts.GetQueryableAsync();
                var entities = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.IsActive
                        && (x.OwnerPhone == null || x.OwnerPhone == "")
                        && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > now)
                        && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value))
                        .OrderByDescending(x => x.CreatedDateTime));
                return entities.Select(MapToDto).ToList();
            });
        }

        /// <summary>
        /// perf-hunt 2026-10-19: batch best-code evaluation for checkout
        /// auto-apply. Loads candidate codes ONCE (fresh, not the 5-min cache)
        /// and reuses <see cref="CheckUsable"/> + <see cref="ComputeDiscount"/>
        /// so the result is identical to validating each code via
        /// <see cref="ValidateAsync(ValidateDiscountInput, string)"/> —
        /// 1 DB query instead of N HTTP round-trips with N DB lookups.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<BestDiscountResultDto> EvaluateBestAsync(
            EvaluateBestDiscountInput input)
        {
            var subtotal = input?.Subtotal ?? 0;
            var customerPhone = input?.CustomerPhone;

            var now = DateTime.UtcNow;
            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive
                    && (x.OwnerPhone == null || x.OwnerPhone == "")
                    && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > now)
                    && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value))
                    .OrderByDescending(x => x.CreatedDateTime));

            string bestCode = null;
            decimal bestAmount = 0;
            foreach (var entity in entities)
            {
                // VoucherType Freeship có luồng riêng — chỉ xét mã giảm tiền,
                // giống hệt vòng lặp client cũ.
                if (entity.VoucherType == VoucherType.Freeship
                    || string.IsNullOrWhiteSpace(entity.Code))
                {
                    continue;
                }

                var (valid, _) = CheckUsable(entity, subtotal, customerPhone);
                if (!valid)
                {
                    continue;
                }

                var amount = ComputeDiscount(entity, subtotal);
                if (amount > bestAmount)
                {
                    bestAmount = amount;
                    bestCode = entity.Code;
                }
            }

            return new BestDiscountResultDto
            {
                Code = bestCode,
                DiscountAmount = bestAmount
            };
        }

        private static (bool Valid, string Message) CheckUsable(
            DiscountCode entity, decimal subtotal, string customerPhone = null)
        {
            // Thứ tự kiểm tra từ rẻ tới "đắt": tồn tại → đang bật → còn hạn →
            // còn lượt → đúng chủ sở hữu → đạt đơn tối thiểu. Message trả về
            // hiển thị trực tiếp cho khách nên viết rõ nguyên nhân.
            if (entity == null)
            {
                return (false, "Mã giảm giá không tồn tại.");
            }

            if (!entity.IsActive)
            {
                return (false, "Mã giảm giá đã bị tắt.");
            }

            if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value < DateTime.UtcNow)
            {
                return (false, "Mã giảm giá đã hết hạn.");
            }

            if (entity.MaxUses.HasValue && entity.UsedCount >= entity.MaxUses.Value)
            {
                return (false, "Mã giảm giá đã hết lượt sử dụng.");
            }

            if (!string.IsNullOrWhiteSpace(entity.OwnerPhone)
                && !PhonesEqual(entity.OwnerPhone, customerPhone))
            {
                return (false, "Mã giảm giá này không dành cho số điện thoại của bạn.");
            }

            if (subtotal < entity.MinOrderAmount)
            {
                return (false, $"Mã này áp dụng cho đơn từ {entity.MinOrderAmount:N0} ₫ trở lên.");
            }

            return (true, "Áp dụng mã giảm giá thành công.");
        }

        /// <summary>Phone equality tolerant of formatting (digits compared, last 9).</summary>
        private static bool PhonesEqual(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return false;
            }

            var da = new string(a.Where(char.IsDigit).ToArray());
            var db = new string(b.Where(char.IsDigit).ToArray());
            if (da.Length < 9 || db.Length < 9)
            {
                return string.Equals(da, db, StringComparison.Ordinal);
            }

            return string.Equals(
                da.Substring(da.Length - 9),
                db.Substring(db.Length - 9),
                StringComparison.Ordinal);
        }

        private async Task<string> GenerateUniqueCodeAsync(string prefix)
        {
            // Bộ ký tự bỏ I/O/0/1 (dễ nhầm lẫn khi khách đọc to), 6 ký tự ngẫu nhiên.
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = Random.Shared; // perf-hunt 2026-10-19: Random.Shared is thread-safe and avoids same-tick seed collisions across concurrent requests

            // Thử tối đa 10 lần sinh ngẫu nhiên không trùng; nếu vẫn trùng
            // (xác suất cực thấp) thì dùng GUID cho chắc, không retry vô hạn.
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var code = prefix + "-" + new string(
                    Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());

                var queryable = await _discounts.GetQueryableAsync();
                var exists = await _asyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.Code == code));
                if (exists == null)
                {
                    return code;
                }
            }

            return prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        private static decimal ComputeDiscount(DiscountCode entity, decimal subtotal)
        {
            // Freeship vouchers give 0 VND discount — the benefit is the
            // zeroed delivery fee at checkout (item 120).
            if (entity.VoucherType == VoucherType.Freeship)
            {
                return 0;
            }

            decimal amount;
            if (entity.DiscountPercent.HasValue)
            {
                // Chặn phần trăm trong 0..100 (admin nhập tay có thể nhầm, vd 200%)
                // và làm tròn về đồng chẵn vì tiền mặt không có số lẻ.
                var percent = Math.Min(100, Math.Max(0, entity.DiscountPercent.Value));
                amount = Math.Round(subtotal * percent / 100, 0);
            }
            else
            {
                amount = entity.DiscountAmount ?? 0;
            }

            // Đảm bảo không âm và không vượt tổng tiền hàng — tránh tổng đơn
            // bị âm khi mã giảm nhiều hơn giá trị giỏ.
            return Math.Min(Math.Max(0, amount), subtotal);
        }

        private static void EnsureValueProvided(CreateDiscountCodeDto dto)
        {
            // Freeship vouchers carry no monetary value — the benefit is the
            // zeroed delivery fee, so no percent/amount is required.
            if (dto.VoucherType == VoucherType.Freeship)
            {
                return;
            }

            if (!dto.DiscountPercent.HasValue && !dto.DiscountAmount.HasValue)
            {
                throw new Domain.Exceptions.DomainException(
                    "Phải nhập phần trăm giảm hoặc số tiền giảm.");
            }
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new Domain.Exceptions.DomainException("Mã giảm giá không được để trống.");
            }

            return code.Trim().ToUpperInvariant();
        }

        private async Task EnsureCodeUniqueAsync(string code, Guid? excludeId)
        {
            var queryable = await _discounts.GetQueryableAsync();
            var exists = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == code && x.Id != excludeId));
            if (exists != null)
            {
                throw new Domain.Exceptions.DomainException($"Mã '{code}' đã tồn tại.");
            }
        }

        private static DiscountCodeDto MapToDto(DiscountCode entity)
        {
            return new DiscountCodeDto
            {
                Id = entity.Id,
                Code = entity.Code,
                DiscountPercent = entity.DiscountPercent,
                DiscountAmount = entity.DiscountAmount,
                MinOrderAmount = entity.MinOrderAmount,
                ExpiresAt = entity.ExpiresAt,
                MaxUses = entity.MaxUses,
                UsedCount = entity.UsedCount,
                IsActive = entity.IsActive,
                OwnerPhone = entity.OwnerPhone,
                VoucherType = entity.VoucherType
            };
        }
    }
}
