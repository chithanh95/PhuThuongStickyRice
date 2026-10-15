using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
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
    }

    public class DiscountAppService : ApplicationService, IDiscountAppService
    {
        private readonly IRepository<DiscountCode, Guid> _discounts;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;

        public DiscountAppService(
            IRepository<DiscountCode, Guid> discounts,
            IAsyncQueryableExecuter asyncExecuter,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger)
        {
            _discounts = discounts;
            _asyncExecuter = asyncExecuter;
            _permissions = permissions;
            _activityLogger = activityLogger;
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<DiscountCodeDto>> GetListAsync()
        {
            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedDateTime));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<DiscountCodeDto> GetAsync(Guid id)
        {
            var entity = await _discounts.GetAsync(id);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<DiscountCodeDto> CreateAsync(CreateDiscountCodeDto dto)
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

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.DiscountCreate, $"Thêm mã giảm giá {entity.Code}.");

            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<DiscountCodeDto> UpdateAsync(Guid id, CreateDiscountCodeDto dto)
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

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.DiscountUpdate, $"Sửa mã giảm giá {entity.Code}.");

            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task DeleteAsync(Guid id)
        {
            // Item 155: managing discount codes needs discounts.manage.
            await _permissions.RequirePermissionAsync(PermissionKeys.DiscountsManage);

            var entity = await _discounts.GetAsync(id);
            await _discounts.DeleteAsync(entity, autoSave: true);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.DiscountDelete, $"Xóa mã giảm giá {entity.Code}.");
        }

        [AllowAnonymous]
        public async Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input)
        {
            return await ValidateAsync(input, null);
        }

        [AllowAnonymous]
        public async Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input, string customerPhone)
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

        public async Task<bool> IsFreeshipAsync(string code, decimal subtotal, string customerPhone)
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
        public async Task<decimal> ApplyAsync(string code, decimal subtotal)
        {
            return await ApplyAsync(code, subtotal, null);
        }

        public async Task<decimal> ApplyAsync(string code, decimal subtotal, string customerPhone)
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

            var amount = ComputeDiscount(entity, subtotal);
            entity.UsedCount += 1;
            await _discounts.UpdateAsync(entity, autoSave: true);
            return amount;
        }

        public async Task<DiscountCodeDto> CreateGeneratedAsync(CreateGeneratedDiscountDto dto)
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
            return MapToDto(entity);
        }

        public async Task<bool> HasUsableCodeAsync(string codePrefix, string ownerPhone)
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

        private static (bool Valid, string Message) CheckUsable(
            DiscountCode entity, decimal subtotal, string customerPhone = null)
        {
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
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();

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
                var percent = Math.Min(100, Math.Max(0, entity.DiscountPercent.Value));
                amount = Math.Round(subtotal * percent / 100, 0);
            }
            else
            {
                amount = entity.DiscountAmount ?? 0;
            }

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
