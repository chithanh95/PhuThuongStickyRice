#nullable enable annotations
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Discounts
{
    /// <summary>
    /// Hợp đồng dịch vụ mã giảm giá: admin CRUD mã, khách kiểm tra/áp mã
    /// ở checkout (Validate/Apply/EvaluateBest), hệ thống tăng lượt dùng
    /// sau khi tạo đơn thành công và tự tạm dừng mã đã hết lượt (item 252).
    /// </summary>
    public interface IDiscountAppService : IApplicationService
    {
        /// <summary>Admin: liệt kê 500 mã giảm giá mới nhất.</summary>
        /// <returns>Danh sách mã giảm giá dưới dạng DTO.</returns>
        Task<List<DiscountCodeDto>> GetListAsync();

        /// <summary>
        /// Item 252 (Admin): mã đã bị hệ thống tự tạm dừng vì dùng hết lượt
        /// (IsActive = false, UsedCount &gt;= MaxUses) — hiện ở trung tâm
        /// thông báo để admin biết mà nạp thêm ngân sách hoặc tạo mã mới.
        /// </summary>
        Task<List<DiscountCodeDto>> GetExhaustedCodesAsync();

        /// <summary>Admin: chi tiết 1 mã giảm giá theo Id.</summary>
        /// <param name="id">Id của mã giảm giá.</param>
        /// <returns>Thông tin mã giảm giá.</returns>
        Task<DiscountCodeDto> GetAsync(Guid id);

        /// <summary>Admin (cần quyền discounts.manage): tạo mã giảm giá mới.</summary>
        /// <param name="dto">Mã, % hoặc số tiền giảm, đơn tối thiểu, hạn dùng, số lượt dùng...</param>
        /// <returns>Mã giảm giá vừa tạo.</returns>
        Task<DiscountCodeDto> CreateAsync(CreateDiscountCodeDto dto);

        /// <summary>Admin (cần quyền discounts.manage): cập nhật mã giảm giá.</summary>
        /// <param name="id">Id mã cần sửa.</param>
        /// <param name="dto">Thông tin mới của mã.</param>
        /// <returns>Mã giảm giá sau khi cập nhật.</returns>
        Task<DiscountCodeDto> UpdateAsync(Guid id, CreateDiscountCodeDto dto);

        /// <summary>Admin (cần quyền discounts.manage): xóa mã giảm giá.</summary>
        /// <param name="id">Id mã cần xóa.</param>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Public: kiểm tra 1 mã giảm giá có áp được cho tổng giỏ hàng không.
        /// </summary>
        /// <param name="input">Mã và tổng tiền giỏ hàng cần kiểm tra.</param>
        /// <returns>Kết quả validate (hợp lệ/không + message + số tiền giảm dự kiến).</returns>
        Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input);

        /// <summary>
        /// Item 139 (Public): các mã công khai đang hoạt động và còn hạn,
        /// còn lượt dùng. Checkout dùng để tự áp mã tốt nhất.
        /// </summary>
        Task<List<DiscountCodeDto>> GetActivePublicAsync();

        /// <summary>
        /// perf-hunt 2026-10-19 (Public): trả mã giảm tiền công khai tốt nhất
        /// cho tổng giỏ trong MỘT lần gọi — thay vòng lặp cũ của client
        /// (GetActivePublicAsync + N x ValidateAsync: N HTTP round-trip + N
        /// lần lookup DB trên đường nóng của checkout). Mã freeship bị bỏ
        /// qua (luồng riêng); mã bằng điểm nhau thì giữ mã mới nhất, y như
        /// vòng lặp cũ của client.
        /// </summary>
        Task<BestDiscountResultDto> EvaluateBestDiscountAsync(EvaluateBestDiscountInput input);

        /// <summary>
        /// True khi mã là voucher freeship còn dùng được (item 120).
        /// Dùng lúc tạo đơn để đưa phí giao hàng về 0.
        /// </summary>
        Task<bool> IsFreeshipAsync(string code, decimal subtotal, string? customerPhone);
        /// <summary>
        /// Lúc tạo đơn: trả số tiền được giảm và tăng bộ đếm lượt dùng.
        /// Ném DomainException khi mã không áp được.
        /// </summary>
        Task<decimal> ApplyAsync(string code, decimal subtotal);

        /// <summary>
        /// Lúc tạo đơn với SĐT chủ mã (mã dùng 1 lần như quay số/sinh nhật
        /// gắn với SĐT). Ném DomainException khi mã không áp được.
        /// </summary>
        Task<decimal> ApplyAsync(string code, decimal subtotal, string? customerPhone);

        /// <summary>
        /// Kiểm tra mã và trả số tiền được giảm mà KHÔNG tăng bộ đếm lượt
        /// dùng. Gọi <see cref="IncrementUsageAsync"/> sau khi tạo đơn thành công.
        /// </summary>
        Task<decimal> ValidateAndComputeAsync(string code, decimal subtotal, string? customerPhone);

        /// <summary>
        /// Gộp <see cref="ValidateAndComputeAsync"/> và <see cref="IsFreeshipAsync"/>
        /// thành 1 query duy nhất: validate mã, tính số tiền giảm và cho biết mã
        /// có phải freeship không. Dùng ở checkout để tránh SELECT cùng 1 row
        /// DiscountCode 2 lần cho mỗi đơn. Ném DomainException khi mã không dùng được.
        /// </summary>
        Task<(decimal DiscountValue, bool IsFreeship)> ValidateAndComputeWithFreeshipAsync(
            string code, decimal subtotal, string? customerPhone);

        /// <summary>
        /// Bug-hunt 2026-10-20: lấy mức đơn tối thiểu (MinOrderAmount) của mã
        /// để kiểm tra lại khi khách tự sửa đơn (bớt món). Trả 0 khi mã không
        /// tồn tại (không còn ràng buộc). Chỉ đọc 1 field, không re-validate
        /// toàn bộ — mã dùng 1 lần (SN-/SPIN-...) đã tăng UsedCount nên
        /// CheckUsable đầy đủ sẽ báo sai là hết lượt.
        /// </summary>
        /// <param name="code">Mã giảm giá (đã chuẩn hóa).</param>
        /// <returns>Mức đơn tối thiểu của mã, hoặc 0 khi mã không tồn tại.</returns>
        Task<decimal> GetMinOrderAmountAsync(string code);

        /// <summary>
        /// Tăng bộ đếm lượt dùng của mã. Gọi sau khi tạo đơn thành công.
        /// </summary>
        Task IncrementUsageAsync(string code);

        /// <summary>
        /// Hoàn lại lượt dùng của mã khi đơn hàng bị hủy (không throw khi mã
        /// trống hoặc không tồn tại — hủy đơn phải luôn thành công). Đơn đã
        /// hủy không còn tiêu thụ lượt, nên mã dùng 1 lần (quà tân thủ, quay
        /// số, sinh nhật...) trả về cho khách dùng lại.
        /// </summary>
        Task RefundUsageAsync(string? code);

        /// <summary>
        /// Hoàn lại lượt dùng của nhiều mã theo batch: gộp các mã trùng (đã
        /// chuẩn hóa) rồi giảm 1 lần cho mỗi mã — dùng ở worker tự hủy đơn để
        /// không tốn 1 round-trip cho mỗi đơn bị hủy. Không throw khi mã trống
        /// hoặc không tồn tại, giống <see cref="RefundUsageAsync"/>.
        /// </summary>
        Task RefundUsageBatchAsync(IEnumerable<string?> codes);

        /// <summary>
        /// Public: kiểm tra 1 mã với tổng giỏ hàng cho 1 SĐT cụ thể.
        /// </summary>
        Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input, string? customerPhone);

        /// <summary>
        /// Tạo mã dùng 1 lần do hệ thống sinh (vòng quay may mắn, voucher
        /// sinh nhật). Internal — các service khác trong application gọi tới.
        /// </summary>
        Task<DiscountCodeDto> CreateGeneratedAsync(CreateGeneratedDiscountDto dto);

        /// <summary>
        /// Internal: tạo hàng loạt mã dùng 1 lần do hệ thống sinh (quy tắc
        /// thưởng khi đơn hoàn tất). Gom toàn bộ insert vào UoW hiện tại
        /// (autoSave: false) và chỉ invalidate cache 1 lần — thay vì mỗi mã
        /// 1 SaveChanges + 1 invalidate toàn bộ cache public như gọi
        /// <see cref="CreateGeneratedAsync"/> trong vòng lặp. Thứ tự kết quả
        /// khớp thứ tự đầu vào; validate từng DTO giống hệt bản đơn.
        /// </summary>
        /// <param name="dtos">Danh sách yêu cầu tạo mã, rỗng thì trả về rỗng.</param>
        Task<List<DiscountCodeDto>> CreateGeneratedBatchAsync(IReadOnlyList<CreateGeneratedDiscountDto> dtos);

        /// <summary>
        /// Internal: true khi SĐT đã có mã còn dùng được với tiền tố cho trước
        /// (chặn sinh trùng mã quay số/sinh nhật).
        /// </summary>
        Task<bool> HasUsableCodeAsync(string codePrefix, string ownerPhone);

        /// <summary>
        /// Internal: tập con của danh sách SĐT đã có mã còn dùng được với
        /// tiền tố cho trước — 1 query gộp thay vì gọi
        /// <see cref="HasUsableCodeAsync"/> cho từng SĐT (tránh N+1 trong
        /// vòng lặp voucher sinh nhật). Điều kiện lọc giống hệt
        /// <see cref="HasUsableCodeAsync"/>; thành viên của set trả về tương
        /// đương kết quả của method đó cho từng SĐT đầu vào.
        /// </summary>
        Task<HashSet<string>> GetPhonesWithUsableCodeAsync(string codePrefix, IEnumerable<string> phones);

        /// <summary>
        /// Item 143 (Internal): mã còn dùng được đầu tiên với tiền tố cho
        /// trước của SĐT, hoặc null khi không có.
        /// </summary>
        Task<string?> GetUsableCodeAsync(string codePrefix, string ownerPhone);

        /// <summary>
        /// Internal: thu hồi voucher trúng từ vòng quay may mắn khi đơn bị
        /// hủy — tắt mã (IsActive = false) để không dùng được nữa. Bỏ qua khi
        /// mã trống/không tồn tại; KHÔNG thu hồi mã đã được dùng thật
        /// (UsedCount &gt; 0 — khách đã đổi voucher lấy đơn thật trước khi
        /// đơn này bị hủy). Chống farm voucher: quay trên đơn Pending rồi hủy
        /// đơn giữ lại voucher (bug-hunt 2026-10-20).
        /// </summary>
        /// <param name="spinPrizeCode">Mã voucher đã lưu trên đơn (Order.SpinPrizeCode).</param>
        Task RevokeSpinPrizeAsync(string? spinPrizeCode);

        /// <summary>
        /// Internal: thu hồi voucher trúng vòng quay cho nhiều đơn bị hủy
        /// theo batch — 1 SELECT gộp (WHERE Code IN) + chỉ 1 lần invalidate
        /// cache public, ngữ nghĩa giống hệt gọi <see cref="RevokeSpinPrizeAsync"/>
        /// cho từng mã (bỏ qua mã trống/không tồn tại/đã tắt/đã dùng thật).
        /// Dùng ở worker tự hủy đơn: mỗi lần gọi đơn lẻ đều đánh sập toàn bộ
        /// cache public (products/ratings/promos...), N đơn trong 1 lần worker
        /// chạy sẽ gây N đợt cache-miss dây chuyền trên các endpoint nóng nhất.
        /// </summary>
        /// <param name="spinPrizeCodes">Mã voucher đã lưu trên các đơn (Order.SpinPrizeCode).</param>
        Task RevokeSpinPrizesAsync(IEnumerable<string?> spinPrizeCodes);
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

        // Độ dài phần ngẫu nhiên của mã sinh tự động (bỏ ký tự dễ nhầm I/O/0/1
        // để khách đọc to không bị nhầm khi nhập mã).
        private const int GeneratedCodeRandomPartLength = 6;
        // Số lần thử sinh ngẫu nhiên tối đa; nếu vẫn trùng (xác suất cực thấp)
        // thì fallback sang GUID thay vì retry vô hạn.
        private const int GeneratedCodeMaxAttempts = 10;

        // Mẫu số quy đổi phần trăm (10% = 10/100); cũng là trần % giảm hợp
        // lệ khi kẹp giá trị admin nhập tay.
        private const int PercentDivisor = 100;

        // perf-hunt 2026-10-19 (run 18:25): trần số mã công khai "đang sống"
        // nạp vào memory mỗi lần checkout. Mã công khai không hạn dùng/không
        // giới hạn lượt (vd "WELCOME10") sống vĩnh viễn nên tập này tăng đơn
        // điệu theo thời gian; query không Take sẽ phình vô hạn trên đường
        // nóng public (EvaluateBestDiscountAsync chạy mỗi lần xem giỏ/checkout).
        // Giữ OrderByDescending(CreatedDateTime) nên 200 mã mới nhất được
        // xét — ngữ nghĩa "mã bằng điểm nhau thì giữ mã mới nhất" không đổi;
        // không shop nào chạy >200 mã công khai đồng thời còn sống.
        private const int MaxPublicCodes = 200;

        // perf-hunt 2026-10-19: bảng DiscountCodes tăng không giới hạn
        // (vòng quay may mắn / sinh nhật / quy tắc thưởng sinh mã cho
        // từng khách). Trang admin không nạp hết để không nặng — chỉ lấy
        // N mã mới nhất.
        private const int MaxAdminListRows = 500;

        // Trang "mã hết lượt" chỉ là cảnh báo để admin biết mã nào cần gia
        // hạn — 50 mã mới nhất là đủ, không cần liệt kê hết lịch sử.
        private const int MaxExhaustedCodes = 50;

        // Kích thước batch khi lọc SĐT theo IN clause. SQL Server giới hạn
        // ~2100 tham số/query — Chunk 1000 giữ biên độ an toàn, kết quả
        // tương đương với không chunk.
        private const int PhoneCheckBatchSize = 1000;

        // [codequality-smell 2026-10-20] Magic number: kích thước chunk khi
        // tra cứu mã sinh trong DB (WHERE Code IN chunk). SQL Server giới
        // hạn ~2100 tham số/query — Chunk 1000 giữ biên độ an toàn, cùng
        // lý do như PhoneCheckBatchSize ở trên.
        private const int DbCollisionCheckChunkSize = 1000;

        /// <summary>
        /// Điều kiện "mã còn dùng được" dùng chung cho cả kiểm tra 1 mã,
        /// kiểm tra 1 SĐT và kiểm tra hàng loạt — gom 1 chỗ để logic lọc
        /// không lệch nhau giữa các method (tiền tố khớp, đang bật, còn hạn,
        /// còn lượt dùng).
        /// </summary>
        /// <param name="prefix">Tiền tố mã (đã chuẩn hóa chữ hoa).</param>
        /// <param name="now">Mốc thời gian so sánh hạn dùng.</param>
        private static Expression<Func<DiscountCode, bool>> UsableCodeFilter(
            string prefix, DateTime now) =>
            x => x.Code.StartsWith(prefix)
                && x.IsActive
                && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > now)
                && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value);

        /// <summary>
        /// Điều kiện "mã công khai đang hoạt động" dùng chung cho
        /// <see cref="GetActivePublicAsync"/> và <see cref="EvaluateBestDiscountAsync"/>
        /// — mã không gắn SĐT riêng, đang bật, còn hạn, còn lượt dùng.
        /// </summary>
        private static Expression<Func<DiscountCode, bool>> PublicActiveCodeFilter(
            DateTime now) =>
            x => x.IsActive
                && string.IsNullOrEmpty(x.OwnerPhone)
                && (!x.ExpiresAt.HasValue || x.ExpiresAt.Value > now)
                && (!x.MaxUses.HasValue || x.UsedCount < x.MaxUses.Value);

        /// <summary>
        /// Khởi tạo service mã giảm giá: repository mã, cache catalog (cho mã
        /// công khai ở giỏ/checkout), kiểm tra quyền và log hoạt động admin.
        /// </summary>
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
            // perf-hunt 2026-10-19: bảng DiscountCodes tăng không giới hạn
            // (vòng quay may mắn / sinh nhật / quy tắc thưởng sinh mã cho
            // từng khách). Giới hạn trang admin 500 mã mới nhất, giống kiểu
            // Take(200) của danh sách ví/loyalty.
            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedDateTime).Take(MaxAdminListRows));
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
        /// Item 252 (Admin): mã tự tạm dừng vì hết lượt dùng. Query SQL trực
        /// tiếp (không qua GetListAsync) để không bị giới hạn 500 mã mới nhất
        /// che mất mã cũ đã hết lượt.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<DiscountCodeDto>> GetExhaustedCodesAsync()
        {
            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => !x.IsActive
                        && x.MaxUses.HasValue
                        && x.UsedCount >= x.MaxUses.Value)
                    .OrderByDescending(x => x.CreatedDateTime)
                    .Take(MaxExhaustedCodes));
            return entities.Select(MapToDto).ToList();
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
            await _activityLogger.LogAsync(StaffActivityActions.DiscountCreate, $"Thêm mã giảm giá {entity.Code}.");

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
            await _activityLogger.LogAsync(StaffActivityActions.DiscountUpdate, $"Sửa mã giảm giá {entity.Code}.");

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
            await _activityLogger.LogAsync(StaffActivityActions.DiscountDelete, $"Xóa mã giảm giá {entity.Code}.");
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
        public virtual async Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input, string? customerPhone)
        {
            var entity = await FindByCodeAsync(input.Code);

            var (valid, message) = CheckUsable(entity, input.Subtotal, customerPhone);
            return new ValidateDiscountResultDto
            {
                IsValid = valid,
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
        public virtual async Task<bool> IsFreeshipAsync(string code, decimal subtotal, string? customerPhone)
        {
            var entity = await FindByCodeAsync(code);

            if (entity == null || entity.VoucherType != VoucherType.Freeship)
            {
                return false;
            }

            var (valid, _) = CheckUsable(entity, subtotal, customerPhone);
            return valid;
        }

        /// <summary>
        /// Lúc tạo đơn: validate mã, tính số tiền được giảm và tăng bộ đếm
        /// lượt dùng. Ném <see cref="Domain.Exceptions.DomainException"/> khi
        /// mã không áp được.
        /// </summary>
        /// <param name="code">Mã giảm giá (khách tự nhập, không gắn SĐT).</param>
        /// <param name="subtotal">Tổng tiền hàng trước khi giảm.</param>
        /// <returns>Số tiền được giảm.</returns>
        /// <summary>
        /// Áp mã giảm giá vào tổng tiền (bản rút gọn không SĐT) — chuyển tiếp
        /// sang bản đầy đủ với customerPhone = null. Dùng cho khách vãng lai;
        /// mã spin/voucher gắn 1 SĐT sẽ bị từ chối ở bước validate.
        /// </summary>
        /// <param name="code">Mã giảm giá đã chuẩn hóa.</param>
        /// <param name="subtotal">Tổng tiền hàng trước khi giảm.</param>
        /// <returns>Số tiền được giảm.</returns>
        public virtual async Task<decimal> ApplyAsync(string code, decimal subtotal)
        {
            return await ApplyAsync(code, subtotal, null);
        }

        /// <summary>
        /// Áp mã giảm giá khi tạo đơn (bản có SĐT chủ sở hữu): validate mã,
        /// tính số tiền giảm và tăng bộ đếm lượt dùng. Mã spin/voucher sinh
        /// nhật chỉ gắn 1 lần dùng cho 1 SĐT nên bắt buộc truyền SĐT để chặn
        /// dùng ké mã của người khác. Ném DomainException khi mã không áp
        /// dụng được.
        /// </summary>
        /// <param name="code">Mã giảm giá đã chuẩn hóa.</param>
        /// <param name="subtotal">Tổng tiền hàng trước khi giảm.</param>
        /// <param name="customerPhone">SĐT chủ đơn (null với khách vãng lai).</param>
        /// <returns>Số tiền được giảm.</returns>
        public virtual async Task<decimal> ApplyAsync(string code, decimal subtotal, string? customerPhone)
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
        public virtual async Task<decimal> ValidateAndComputeAsync(string code, decimal subtotal, string? customerPhone)
        {
            var entity = await FindByCodeAsync(code);

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
            string code, decimal subtotal, string? customerPhone)
        {
            var entity = await FindByCodeAsync(code);

            var (valid, message) = CheckUsable(entity, subtotal, customerPhone);
            if (!valid)
            {
                throw new Domain.Exceptions.DomainException(message);
            }

            return (ComputeDiscount(entity, subtotal), entity.VoucherType == VoucherType.Freeship);
        }

        /// <summary>
        /// Bug-hunt 2026-10-20: lấy mức đơn tối thiểu của mã (xem interface).
        /// </summary>
        public virtual async Task<decimal> GetMinOrderAmountAsync(string code)
        {
            var entity = await FindByCodeAsync(code);
            return entity?.MinOrderAmount ?? 0;
        }

        /// <summary>
        /// Tăng UsedCount lên 1 cho mã. Chỉ gọi sau khi tạo đơn thành công —
        /// không tự ý tăng ở bước tính thử, kẻo đơn lỗi vẫn đốt lượt mã.
        /// </summary>
        public virtual async Task IncrementUsageAsync(string code)
        {
            // Chuẩn hóa 1 lần, dùng chung cho cả bước kiểm tra tồn tại lẫn câu UPDATE.
            var normalized = NormalizeCode(code);
            var queryable = await _discounts.GetQueryableAsync();
            // [codequality-practices 2026-10-19] Bỏ FindByCodeAsync nạp cả entity
            // chỉ để check null — AnyAsync đủ để biết mã có tồn tại không.
            if (!await _asyncExecuter.AnyAsync(queryable, x => x.Code == normalized))
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
        /// Hoàn lại lượt dùng khi đơn bị hủy: giảm UsedCount nguyên tử tại
        /// DB, clamp không về âm. Không throw khi mã trống hoặc không còn
        /// tồn tại (đơn đã hủy phải thành công) — mã lưu trên Order là text
        /// tự do, admin có thể đã xóa mã sau khi đơn tạo.
        /// </summary>
        public virtual async Task RefundUsageAsync(string? code)
        {
            var normalized = code?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            await _discounts.TryDecrementUsageAsync(normalized);
        }

        /// <summary>
        /// Hoàn lượt dùng của nhiều mã theo batch: chuẩn hóa từng mã, gộp số
        /// lượt theo mã rồi gọi 1 UPDATE cho mỗi mã khác nhau — kết quả giống
        /// hệt gọi <see cref="RefundUsageAsync"/> cho từng mã (không về âm,
        /// bỏ qua mã trống), nhưng chỉ tốn 1 round-trip/mã.
        /// </summary>
        public virtual async Task RefundUsageBatchAsync(IEnumerable<string?> codes)
        {
            if (codes == null)
            {
                return;
            }

            var decrements = new Dictionary<string, int>();
            foreach (var code in codes)
            {
                var normalized = code?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(normalized))
                {
                    continue;
                }

                decrements.TryGetValue(normalized, out var current);
                decrements[normalized] = current + 1;
            }

            await _discounts.TryDecrementUsageBatchAsync(decrements);
        }

        /// <summary>
        /// Thu hồi voucher trúng từ vòng quay may mắn khi đơn bị hủy: tắt mã
        /// để không dùng được nữa. Không throw khi mã trống/không tồn tại
        /// (đơn hủy phải thành công). Mã đã dùng thật (UsedCount &gt; 0) được
        /// giữ nguyên — khách đã đổi voucher lấy đơn thật trước khi đơn này
        /// bị hủy thì đó là giao dịch hợp lệ, không phải farm.
        /// </summary>
        public virtual async Task RevokeSpinPrizeAsync(string? spinPrizeCode)
        {
            var normalized = spinPrizeCode?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            var entity = await FindByCodeAsync(normalized);
            if (entity == null || !entity.IsActive || entity.UsedCount > 0)
            {
                return;
            }

            entity.IsActive = false;
            await _discounts.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Thu hồi voucher trúng vòng quay cho nhiều đơn bị hủy theo batch:
        /// chuẩn hóa + gom mã trùng, 1 SELECT (WHERE Code IN) thay vì N lần
        /// FindByCodeAsync, rồi tắt các mã còn hiệu lực. Chỉ invalidate cache
        /// public 1 lần khi có ít nhất 1 mã bị thu hồi — gọi đơn lẻ từng mã
        /// sẽ đánh sập toàn bộ cache nóng (products/ratings/promos...) mỗi
        /// lần, gây cache-miss dây chuyền. Ngữ nghĩa giống hệt gọi
        /// <see cref="RevokeSpinPrizeAsync"/> cho từng mã: bỏ qua mã trống,
        /// mã không tồn tại, mã đã tắt, mã đã dùng thật (UsedCount &gt; 0);
        /// không throw — đơn hủy phải luôn thành công.
        /// </summary>
        public virtual async Task RevokeSpinPrizesAsync(IEnumerable<string?> spinPrizeCodes)
        {
            if (spinPrizeCodes == null)
            {
                return;
            }

            // Chuẩn hóa giống hệt RevokeSpinPrizeAsync (không dùng
            // NormalizeCode vì nó ném khi mã trống — ở đây chỉ bỏ qua).
            var codes = spinPrizeCodes
                .Select(c => c?.Trim().ToUpperInvariant())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            if (codes.Count == 0)
            {
                return;
            }

            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => codes.Contains(x.Code)));

            var revokedAny = false;
            foreach (var entity in entities)
            {
                if (!entity.IsActive || entity.UsedCount > 0)
                {
                    continue;
                }

                entity.IsActive = false;
                // perf-hunt 2026-10-20: autoSave: false — entity đã tracked,
                // UoW (virtual method) commit 1 lần duy nhất cuối method thay vì
                // N lần SaveChanges (N roundtrip + N commit) cho mỗi mã.
                // Ngữ nghĩa không đổi: vẫn tắt đúng tập mã, vẫn invalidate
                // cache 1 lần.
                await _discounts.UpdateAsync(entity, autoSave: false);
                revokedAny = true;
            }

            if (revokedAny)
            {
                _catalogCache.Invalidate();
            }
        }

        /// <summary>
        /// Tạo mã dùng 1 lần do hệ thống sinh (vòng quay may mắn, quà sinh
        /// nhật): mã ngẫu nhiên theo tiền tố, MaxUses = 1, gắn SĐT chủ sở hữu
        /// để không ai dùng ké. Chỉ các service nội bộ gọi.
        /// </summary>
        public virtual async Task<DiscountCodeDto> CreateGeneratedAsync(CreateGeneratedDiscountDto dto)
        {
            var entity = await BuildGeneratedEntityAsync(dto);
            await _discounts.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Internal: tạo hàng loạt mã dùng 1 lần do hệ thống sinh. Mỗi DTO
        /// validate + sinh mã duy nhất y hệt <see cref="CreateGeneratedAsync"/>,
        /// nhưng gom insert vào UoW hiện tại (autoSave: false — ABP flush 1 lần
        /// khi UoW hoàn thành) và invalidate cache đúng 1 lần. Không đổi dữ
        /// liệu tạo ra: cùng prefix/mã/owner/hạn dùng.
        /// </summary>
        public virtual async Task<List<DiscountCodeDto>> CreateGeneratedBatchAsync(
            IReadOnlyList<CreateGeneratedDiscountDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return new List<DiscountCodeDto>();
            }

            // Validate toàn bộ DTO trước khi chạm DB — DTO lỗi vẫn ném
            // DomainException như bản đơn, không tạo mã nào.
            var prefixes = new List<string>(dtos.Count);
            foreach (var dto in dtos)
            {
                ValidateGeneratedDiscountDto(dto);
                prefixes.Add(NormalizeGeneratedCodePrefix(dto));
            }

            // perf-hunt 2026-10-20: N mã ứng viên sinh trong memory rồi kiểm
            // tra trùng 1 lần bằng query WHERE Code IN (chunk 1000) thay vì
            // 1 query kiểm tra trùng/mã — tiết kiệm N-1 roundtrip DB mỗi đợt.
            var codes = await GenerateUniqueCodesAsync(prefixes);

            var entities = new List<DiscountCode>(dtos.Count);
            for (var i = 0; i < dtos.Count; i++)
            {
                entities.Add(BuildGeneratedEntity(dtos[i], codes[i]));
            }

            // perf-hunt 2026-10-20: gom insert (không SaveChanges từng mã) và
            // chỉ 1 lần _catalogCache.Invalidate() cho cả batch — trước đây
            // mỗi rule thưởng là 1 SaveChanges + 1 invalidate toàn bộ cache
            // public (products, ratings, FAQs, promos...) trên đường hoàn tất
            // đơn. Mã đã sinh phía client nên không cần SaveChanges để lấy.
            foreach (var entity in entities)
            {
                await _discounts.InsertAsync(entity, autoSave: false);
            }

            _catalogCache.Invalidate();
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Kiểm tra DTO mã sinh tự động: phải có phần trăm giảm hoặc số tiền
        /// giảm. Ném <see cref="Domain.Exceptions.DomainException"/> khi DTO
        /// không hợp lệ (giống bản cũ để bản đơn và bản batch không lệch nhau).
        /// </summary>
        /// <param name="dto">DTO cần kiểm tra.</param>
        private static void ValidateGeneratedDiscountDto(CreateGeneratedDiscountDto dto)
        {
            if (!dto.DiscountPercent.HasValue && !dto.DiscountAmount.HasValue)
            {
                throw new Domain.Exceptions.DomainException(
                    "Phải nhập phần trăm giảm hoặc số tiền giảm.");
            }
        }

        /// <summary>
        /// Chuẩn hóa tiền tố mã sinh tự động (trim + chữ hoa), mặc định
        /// "VOUCHER" khi DTO không đặt tiền tố.
        /// </summary>
        /// <param name="dto">DTO chứa tiền tố cần chuẩn hóa.</param>
        /// <returns>Tiền tố đã chuẩn hóa.</returns>
        private static string NormalizeGeneratedCodePrefix(CreateGeneratedDiscountDto dto)
            => string.IsNullOrWhiteSpace(dto.Prefix) ? "VOUCHER" : dto.Prefix.Trim().ToUpperInvariant();

        /// <summary>
        /// Validate DTO + sinh mã duy nhất rồi dựng entity DiscountCode dùng
        /// 1 lần (MaxUses = 1, gắn SĐT chủ sở hữu). Bản đơn giữ nguyên logic
        /// cũ; bản batch dùng <see cref="GenerateUniqueCodesAsync"/> để gom
        /// bước kiểm tra trùng.
        /// </summary>
        private async Task<DiscountCode> BuildGeneratedEntityAsync(CreateGeneratedDiscountDto dto)
        {
            ValidateGeneratedDiscountDto(dto);
            var code = await GenerateUniqueCodeAsync(NormalizeGeneratedCodePrefix(dto));
            return BuildGeneratedEntity(dto, code);
        }

        /// <summary>
        /// Dựng entity DiscountCode dùng 1 lần từ DTO đã validate và mã đã
        /// được đảm bảo duy nhất. Dùng chung cho bản đơn và bản batch để
        /// 2 luồng không lệch nhau.
        /// </summary>
        /// <param name="dto">DTO đã validate.</param>
        /// <param name="code">Mã giảm giá đã đảm bảo duy nhất.</param>
        /// <returns>Entity DiscountCode dùng 1 lần.</returns>
        private static DiscountCode BuildGeneratedEntity(CreateGeneratedDiscountDto dto, string code)
        {
            return new DiscountCode
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
        }

        /// <summary>
        /// Chuẩn hóa tiền tố mã giảm giá (trim + chữ hoa), hoặc null khi tiền
        /// tố rỗng — gom 1 dòng trùng lặp ở các method tìm mã theo tiền tố.
        /// </summary>
        private static string? NormalizeCodePrefix(string codePrefix)
            => string.IsNullOrWhiteSpace(codePrefix) ? null : codePrefix.Trim().ToUpperInvariant();

        /// <summary>
        /// True khi SĐT đã có mã còn dùng được với tiền tố cho trước — chống
        /// cấp trùng mã quay/sinh nhật cho cùng 1 khách.
        /// </summary>
        /// <summary>
        /// Internal: true khi SĐT còn ít nhất 1 mã dùng được với tiền tố cho
        /// trước (đang bật, còn hạn, còn lượt dùng). Chỉ SELECT 1 (AnyAsync)
        /// thay vì tải entity — dùng kiểm tra nhanh trước khi sinh mã mới.
        /// </summary>
        /// <param name="codePrefix">Tiền tố mã cần kiểm tra.</param>
        /// <param name="ownerPhone">SĐT chủ sở hữu mã.</param>
        /// <returns>true khi còn mã dùng được.</returns>
        public virtual async Task<bool> HasUsableCodeAsync(string codePrefix, string ownerPhone)
        {
            var prefix = NormalizeCodePrefix(codePrefix);
            if (prefix == null || string.IsNullOrWhiteSpace(ownerPhone))
            {
                return false;
            }

            var queryable = await _discounts.GetQueryableAsync();
            // Chỉ cần biết "có tồn tại mã nào không" nên dùng AnyAsync —
            // không materialize cả row entity vào bộ nhớ như FirstOrDefaultAsync.
            return await _asyncExecuter.AnyAsync(
                queryable
                    .Where(UsableCodeFilter(prefix, DateTime.UtcNow))
                    .Where(x => x.OwnerPhone == ownerPhone));
        }

        /// <summary>
        /// Batch version của <see cref="HasUsableCodeAsync"/>: trả về các SĐT
        /// trong danh sách đã có mã còn dùng được với tiền tố cho trước —
        /// gom theo batch 1000 SĐT (IN clause) thay vì 1 query/SĐT. Điều kiện
        /// lọc giữ nguyên như HasUsableCodeAsync nên kết quả tương đương
        /// từng phần tử.
        /// </summary>
        public virtual async Task<HashSet<string>> GetPhonesWithUsableCodeAsync(
            string codePrefix, IEnumerable<string> phones)
        {
            var result = new HashSet<string>();
            var prefix = NormalizeCodePrefix(codePrefix);
            if (prefix == null)
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

            var queryable = await _discounts.GetQueryableAsync();
            // Chunk IN clause theo batch 1000 (SQL Server giới hạn ~2100
            // params) — cùng pattern GetTagsByPhonesAsync; caller tương lai
            // truyền >2100 SĐT sẽ không còn SqlException. Kết quả tương đương.
            foreach (var batch in list.Chunk(PhoneCheckBatchSize))
            {
                var rows = await _asyncExecuter.ToListAsync(
                    queryable
                        .Where(UsableCodeFilter(prefix, DateTime.UtcNow))
                        .Where(x => batch.Contains(x.OwnerPhone))
                        .Select(x => x.OwnerPhone));
                foreach (var phone in rows)
                {
                    result.Add(phone);
                }
            }

            return result;
        }

        /// <summary>
        /// Item 143 (Internal): mã còn dùng được đầu tiên với tiền tố cho trước
        /// của SĐT, hoặc null khi không có (dùng kiểm tra "khách đã có voucher
        /// đơn đầu chưa" trước khi sinh thêm mã).
        /// </summary>
        /// <param name="codePrefix">Tiền tố mã (đã chuẩn hóa chữ hoa).</param>
        /// <param name="ownerPhone">SĐT chủ sở hữu mã.</param>
        /// <returns>Mã đầu tiên còn dùng được, hoặc null.</returns>
        public virtual async Task<string?> GetUsableCodeAsync(string codePrefix, string ownerPhone)
        {
            var prefix = NormalizeCodePrefix(codePrefix);
            if (prefix == null || string.IsNullOrWhiteSpace(ownerPhone))
            {
                return null;
            }

            var queryable = await _discounts.GetQueryableAsync();
            // perf-hunt 2026-10-20: chỉ cần mã (string) nên project trực
            // tiếp cột Code thay vì materialize cả entity DiscountCode
            // (tracked) rồi mới đọc .Code — cùng filter, cùng kết quả.
            return await _asyncExecuter.FirstOrDefaultAsync(
                queryable
                    .Where(UsableCodeFilter(prefix, DateTime.UtcNow))
                    .Where(x => x.OwnerPhone == ownerPhone)
                    .Select(x => x.Code));
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
                    queryable
                        .Where(PublicActiveCodeFilter(now))
                        .OrderByDescending(x => x.CreatedDateTime)
                        .Take(MaxPublicCodes));
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
        public virtual async Task<BestDiscountResultDto> EvaluateBestDiscountAsync(
            EvaluateBestDiscountInput input)
        {
            var subtotal = input?.Subtotal ?? 0;
            var customerPhone = input?.CustomerPhone;

            var now = DateTime.UtcNow;
            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(PublicActiveCodeFilter(now))
                    .OrderByDescending(x => x.CreatedDateTime)
                    .Take(MaxPublicCodes));

            var (bestCode, bestAmount) = PickBestDiscountCode(entities, subtotal, customerPhone);

            return new BestDiscountResultDto
            {
                Code = bestCode,
                DiscountAmount = bestAmount
            };
        }

        /// <summary>
        /// Chọn trong danh sách mã công khai đang kích hoạt mã cho số tiền
        /// giảm lớn nhất trên tổng giỏ hàng: mã phải vượt <see cref="CheckUsable"/>
        /// và được tính bằng <see cref="ComputeDiscount"/> nên kết quả khớp
        /// đúng với validate từng mã riêng lẻ. Tách vòng lặp chọn mã tốt nhất
        /// khỏi <see cref="EvaluateBestDiscountAsync"/> (query DB) để mỗi method làm
        /// đúng 1 việc.
        /// </summary>
        /// <param name="entities">Các mã công khai đang kích hoạt (đã sắp xếp mới nhất trước).</param>
        /// <param name="subtotal">Tổng tiền giỏ hàng.</param>
        /// <param name="customerPhone">SĐT khách dùng mã (kiểm tra mã gắn SĐT).</param>
        /// <returns>(Mã tốt nhất, số tiền giảm); không mã nào hợp lệ trả (null, 0).</returns>
        private static (string? Code, decimal Amount) PickBestDiscountCode(
            IReadOnlyList<DiscountCode> entities, decimal subtotal, string? customerPhone)
        {
            string? bestCode = null;
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

            return (bestCode, bestAmount);
        }

        /// <summary>
        /// Kiểm tra 1 mã còn dùng được không theo 6 điều kiện: tồn tại, đang
        /// bật, còn hạn, còn lượt dùng, đúng SĐT chủ mã, đạt đơn tối thiểu.
        /// Thứ tự kiểm tra từ rẻ tới đắt (tồn tại → đang bật → còn hạn → còn
        /// lượt → đúng chủ sở hữu → đạt đơn tối thiểu) để fail nhanh; message
        /// trả về hiển thị trực tiếp cho khách nên viết rõ nguyên nhân.
        /// </summary>
        /// <param name="entity">Entity mã giảm giá; null = mã không tồn tại.</param>
        /// <param name="subtotal">Tổng tiền giỏ hàng.</param>
        /// <param name="customerPhone">SĐT khách dùng mã (kiểm tra mã gắn SĐT).</param>
        /// <returns>(Có dùng được không, message hiển thị trực tiếp cho khách).</returns>
        private static (bool IsValid, string Message) CheckUsable(
            DiscountCode? entity, decimal subtotal, string? customerPhone = null)
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
                && !PhoneNumber.PhonesMatch(entity.OwnerPhone, customerPhone))
            {
                return (false, "Mã giảm giá này không dành cho số điện thoại của bạn.");
            }

            if (subtotal < entity.MinOrderAmount)
            {
                return (false, $"Mã này áp dụng cho đơn từ {entity.MinOrderAmount:N0} ₫ trở lên.");
            }

            return (true, "Áp dụng mã giảm giá thành công.");
        }

        /// <summary>
        /// Sinh mã dùng 1 lần duy nhất theo tiền tố (vd "THUONG-AB12CD") cho
        /// vòng quay may mắn / quà sinh nhật.
        /// </summary>
        /// <param name="prefix">Tiền tố mã (đã chuẩn hóa in hoa).</param>
        /// <returns>Mã chưa từng tồn tại trong DB.</returns>
        /// <summary>
        /// Sinh 1 mã ứng viên theo tiền tố (chưa kiểm tra trùng): tiền tố +
        /// phần ngẫu nhiên mật mã (<see cref="RandomNumberGenerator"/>),
        /// bộ ký tự bỏ I/O/0/1 để khách đọc to không nhầm khi nhập mã.
        /// </summary>
        /// <param name="prefix">Tiền tố mã (đã chuẩn hóa in hoa).</param>
        /// <returns>Mã ứng viên dạng "PREFIX-XXXXXX".</returns>
        private static string BuildGeneratedCodeCandidate(string prefix)
        {
            // Bộ ký tự bỏ I/O/0/1 (dễ nhầm lẫn khi khách đọc to).
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            // CA5394: mã giảm giá quy ra tiền nên phải dùng RNG mật mã —
            // Random.Shared đoán được (deterministic PRNG), kẻ xấu có thể dò mã.
            return prefix + "-" + new string(
                Enumerable.Range(0, GeneratedCodeRandomPartLength).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
        }

        /// <summary>
        /// Fallback khi hết số lần thử sinh ngẫu nhiên (xác suất cực thấp):
        /// hậu tố từ GUID thay vì retry vô hạn (không kiểm tra DB, như bản cũ).
        /// </summary>
        /// <param name="prefix">Tiền tố mã (đã chuẩn hóa in hoa).</param>
        /// <returns>Mã dạng "PREFIX-XXXXXX" từ GUID.</returns>
        private static string BuildGeneratedCodeFallback(string prefix)
            // codequality-team 2026-10-20 run-0245 Agent3-BestPractices: range
            // operator thay Substring(0, N) — ToString("N") dài 32 ký tự ≥ 6.
            => prefix + "-" + Guid.NewGuid().ToString("N")[..GeneratedCodeRandomPartLength].ToUpperInvariant();

        private async Task<string> GenerateUniqueCodeAsync(string prefix)
        {
            var queryable = await _discounts.GetQueryableAsync();

            for (var attempt = 0; attempt < GeneratedCodeMaxAttempts; attempt++)
            {
                var code = BuildGeneratedCodeCandidate(prefix);

                // [codequality-practices 2026-10-19] Mã vừa sinh đã ở dạng chuẩn
                // hóa (prefix + ký tự hoa, không khoảng trắng) nên kiểm tra tồn tại
                // trực tiếp bằng AnyAsync — bỏ FindByCodeAsync nạp cả entity chỉ để check null.
                if (!await _asyncExecuter.AnyAsync(queryable, x => x.Code == code))
                {
                    return code;
                }
            }

            return BuildGeneratedCodeFallback(prefix);
        }

        /// <summary>
        /// perf-hunt 2026-10-20: sinh hàng loạt mã duy nhất cho bản batch —
        /// toàn bộ mã ứng viên được sinh trong memory rồi kiểm tra trùng 1 lần
        /// bằng query WHERE Code IN (chunk 1000 tham số), thay vì 1 query/mã.
        /// Chỉ các mã bị đụng độ mới được sinh lại và kiểm tra tiếp. Ngữ nghĩa
        /// giữ nguyên như bản đơn <see cref="GenerateUniqueCodeAsync"/>: cùng
        /// tiền tố/định dạng, cùng giới hạn số lần thử, cùng fallback GUID khi
        /// hết lần thử; DB vẫn là chốt chặn cuối qua unique index
        /// IX_DiscountCodes_Code.
        /// </summary>
        /// <param name="prefixes">Tiền tố (đã chuẩn hóa) của từng mã, giữ đúng thứ tự.</param>
        /// <returns>Danh sách mã duy nhất, thứ tự khớp <paramref name="prefixes"/>.</returns>
        private async Task<List<string>> GenerateUniqueCodesAsync(IReadOnlyList<string> prefixes)
        {
            var codes = prefixes.Select(BuildGeneratedCodeCandidate).ToList();
            var used = DeduplicateBatchCodes(prefixes, codes);

            var queryable = await _discounts.GetQueryableAsync();
            var pending = new HashSet<int>(Enumerable.Range(0, codes.Count));
            for (var attempt = 0; attempt < GeneratedCodeMaxAttempts && pending.Count > 0; attempt++)
            {
                var collided = await FindCollidedIndexesAsync(queryable, pending, codes);
                if (collided.Count == 0)
                {
                    pending.Clear();
                    break;
                }

                RegenerateCollidedCodes(prefixes, codes, used, collided);
                pending = new HashSet<int>(collided);
            }

            // Hết số lần thử mà vẫn đụng: fallback GUID như bản đơn (không check DB).
            foreach (var codeIndex in pending)
            {
                codes[codeIndex] = BuildGeneratedCodeFallback(prefixes[codeIndex]);
            }

            return codes;
        }

        /// <summary>
        /// Loại mã trùng nhau ngay trong batch sinh. Xác suất trùng ~0 nhưng
        /// vẫn phải chặn để không vỡ unique-index ở bước insert. Trả về tập
        /// mã đã dùng để vòng sinh lại sau giữ tính duy nhất trong batch.
        /// </summary>
        /// <param name="prefixes">Tiền tố của từng mã, giữ đúng thứ tự.</param>
        /// <param name="codes">Danh sách mã ứng viên (sửa tại chỗ khi trùng).</param>
        /// <returns>Tập mã đã dùng (so sánh không phân biệt hoa thường).</returns>
        private static HashSet<string> DeduplicateBatchCodes(IReadOnlyList<string> prefixes, List<string> codes)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < codes.Count; i++)
            {
                while (!used.Add(codes[i]))
                {
                    codes[i] = BuildGeneratedCodeCandidate(prefixes[i]);
                }
            }
            return used;
        }

        /// <summary>
        /// Tra cứu các mã đang chờ trong DB theo từng chunk (giữ dưới giới
        /// hạn tham số của SQL Server), trả về index các mã bị đụng.
        /// </summary>
        /// <param name="queryable">Query mã giảm giá.</param>
        /// <param name="pending">Index các mã cần kiểm tra.</param>
        /// <param name="codes">Danh sách mã ứng viên.</param>
        /// <returns>Index các mã đã tồn tại trong DB.</returns>
        private async Task<List<int>> FindCollidedIndexesAsync(
            IQueryable<DiscountCode> queryable, HashSet<int> pending, List<string> codes)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Best practice LINQ: Chunk nhận IEnumerable và stream từng
            // chunk — không cần .ToList() materialize cả danh sách trước
            // (bản cũ cấp phát 1 List trung gian vô ích mỗi attempt).
            foreach (var chunk in pending.Select(i => codes[i]).Chunk(DbCollisionCheckChunkSize))
            {
                var found = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => chunk.Contains(x.Code)).Select(x => x.Code));
                foreach (var code in found)
                {
                    existing.Add(code);
                }
            }

            return pending.Where(i => existing.Contains(codes[i])).ToList();
        }

        /// <summary>
        /// Sinh lại các mã bị đụng DB, vẫn giữ tính duy nhất trong batch
        /// (mã cũ được gỡ khỏi tập đã dùng trước khi sinh mã mới).
        /// </summary>
        /// <param name="prefixes">Tiền tố của từng mã, giữ đúng thứ tự.</param>
        /// <param name="codes">Danh sách mã ứng viên (sửa tại chỗ).</param>
        /// <param name="used">Tập mã đã dùng trong batch.</param>
        /// <param name="collided">Index các mã bị đụng cần sinh lại.</param>
        private static void RegenerateCollidedCodes(
            IReadOnlyList<string> prefixes, List<string> codes, HashSet<string> used, List<int> collided)
        {
            // Sinh lại mã bị đụng, vẫn giữ tính duy nhất trong batch.
            foreach (var collidedIndex in collided)
            {
                used.Remove(codes[collidedIndex]);
                do
                {
                    codes[collidedIndex] = BuildGeneratedCodeCandidate(prefixes[collidedIndex]);
                }
                while (!used.Add(codes[collidedIndex]));
            }
        }

        /// <summary>
        /// Tính số tiền được giảm cho 1 mã trên tổng tiền giỏ hàng.
        /// </summary>
        /// <param name="entity">Mã giảm giá (đã được kiểm tra hợp lệ).</param>
        /// <param name="subtotal">Tổng tiền hàng.</param>
        /// <returns>
        /// Số tiền giảm (VND). Phần trăm bị kẹp trong 0..100 (admin nhập tay có thể nhầm)
        /// và làm tròn về đồng chẵn; kết quả bị chặn không vượt subtotal để tổng đơn không bị âm.
        /// </returns>
        private static decimal ComputeDiscount(DiscountCode entity, decimal subtotal)
        {
            // Mã freeship không trừ tiền — lợi ích của nó là phí giao hàng
            // về 0 ở checkout (item 120).
            if (entity.VoucherType == VoucherType.Freeship)
            {
                return 0;
            }

            decimal amount;
            if (entity.DiscountPercent.HasValue)
            {
                // Chặn phần trăm trong 0..100 (admin nhập tay có thể nhầm, vd 200%)
                // và làm tròn về đồng chẵn vì tiền mặt không có số lẻ.
                var percent = Math.Min(PercentDivisor, Math.Max(0, entity.DiscountPercent.Value));
                amount = Math.Round(subtotal * percent / PercentDivisor, 0);
            }
            else
            {
                amount = entity.DiscountAmount ?? 0;
            }

            // Đảm bảo không âm và không vượt tổng tiền hàng — tránh tổng đơn
            // bị âm khi mã giảm nhiều hơn giá trị giỏ.
            return Math.Min(Math.Max(0, amount), subtotal);
        }

        /// <summary>
        /// Đảm bảo mã có giá trị giảm: phải có % hoặc số tiền giảm.
        /// Mã freeship được miễn vì lợi ích của nó là miễn phí ship, không phải tiền giảm.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo/sửa mã.</param>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi thiếu cả % và số tiền giảm.</exception>
        private static void EnsureValueProvided(CreateDiscountCodeDto dto)
        {
            // Mã freeship không mang giá trị tiền — lợi ích của nó là miễn
            // phí giao hàng, nên không cần nhập %/số tiền giảm.
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

        /// <summary>
        /// Chuẩn hóa mã: cắt khoảng trắng hai đầu và chuyển in hoa, để so khớp
        /// mã không phân biệt hoa/thường khi khách nhập.
        /// </summary>
        /// <param name="code">Mã thô người dùng nhập.</param>
        /// <returns>Mã đã chuẩn hóa.</returns>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi mã trống.</exception>
        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new Domain.Exceptions.DomainException("Mã giảm giá không được để trống.");
            }

            return code.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Tìm mã giảm giá theo mã (đã chuẩn hóa trong hàm): 1 query duy nhất
        /// so khớp chính xác trên cột Code. Trả null khi mã không tồn tại —
        /// nơi gọi tự quyết định: validate thì ném lỗi, còn tăng lượt dùng
        /// thì bỏ qua vì đơn đã được validate trước khi tạo.
        /// </summary>
        /// <param name="code">Mã giảm giá thô (sẽ chuẩn hóa trước khi tìm).</param>
        /// <returns>Entity mã giảm giá, hoặc null khi không tồn tại.</returns>
        private async Task<DiscountCode?> FindByCodeAsync(string code)
        {
            var normalized = NormalizeCode(code);
            var queryable = await _discounts.GetQueryableAsync();
            // [codequality-practices 2026-10-19] Truyền predicate thẳng vào
            // FirstOrDefaultAsync thay vì bọc queryable.Where(...) thừa.
            return await _asyncExecuter.FirstOrDefaultAsync(queryable, x => x.Code == normalized);
        }

        /// <summary>
        /// Đảm bảo mã chưa tồn tại trong DB. Khi sửa mã, loại trừ chính nó
        /// (excludeId) để đổi các field khác không bị báo trùng oan.
        /// </summary>
        /// <param name="code">Mã đã chuẩn hóa.</param>
        /// <param name="excludeId">Id bỏ qua khi kiểm tra trùng (dùng khi Update).</param>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi mã đã tồn tại.</exception>
        private async Task EnsureCodeUniqueAsync(string code, Guid? excludeId)
        {
            var queryable = await _discounts.GetQueryableAsync();
            // [codequality-practices 2026-10-19] Chỉ cần biết mã đã tồn tại
            // hay chưa — AnyAsync (SELECT 1) gọn hơn nạp cả entity chỉ để check null.
            var exists = await _asyncExecuter.AnyAsync(
                queryable, x => x.Code == code && x.Id != excludeId);
            if (exists)
            {
                throw new Domain.Exceptions.DomainException($"Mã '{code}' đã tồn tại.");
            }
        }

        /// <summary>Chuyển entity <c>DiscountCode</c> sang DTO trả về cho client.</summary>
        /// <param name="entity">Entity mã giảm giá.</param>
        /// <returns>DTO mã giảm giá.</returns>
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
