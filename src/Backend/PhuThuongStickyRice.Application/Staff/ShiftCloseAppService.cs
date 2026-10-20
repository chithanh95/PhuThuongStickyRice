
// CS8632: file dùng annotation nullable (?); bật ngữ cảnh annotations để
// compiler hiểu đúng ý nghĩa, không bật phân tích luồng (dự án chưa bật nullable toàn cục).
#nullable enable annotations

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Application.Permissions;

namespace PhuThuongStickyRice.Application.Staff
{
    /// <summary>
    /// Hợp đồng chốt ca (Item 129): đối soát tiền mặt COD cuối ca. Hệ thống tự
    /// tính số tiền lẽ ra phải thu trong ngày (tổng đơn COD đã hoàn tất, xem
    /// <c>GetExpectedCodAsync</c>); nhân viên nhập số tiền thực thu ở
    /// <c>CloseAsync</c> để ghi chênh lệch. Gọi lại cùng ngày chỉ cập nhật bản
    /// ghi cũ (không tạo trùng).
    /// </summary>
    public interface IShiftCloseAppService : IApplicationService
    {
        /// <summary>
        /// Số bản ghi lịch sử chốt ca mặc định khi không truyền <c>take</c> —
        /// màn hình admin chỉ cần xem vài chục dòng gần nhất, không kéo hết lịch sử.
        /// </summary>
        const int DefaultHistoryRows = 30;

        /// <summary>
        /// Trần số bản ghi lịch sử chốt ca cho 1 request — chặn <c>take</c> quá
        /// lớn để 1 request không materialize toàn bộ lịch sử chốt ca.
        /// </summary>
        const int MaxHistoryRows = 100;
        /// <summary>Expected COD for a business day (VN date).</summary>
        Task<decimal> GetExpectedCodAsync(DateTime date);

        /// <summary>
        /// Lấy bản chốt ca của 1 ngày (null nếu ngày đó chưa chốt).
        /// </summary>
        /// <param name="date">Ngày kinh doanh (giờ VN, chỉ lấy phần ngày).</param>
        // [codequality-practices 2026-10-20] Task<ShiftCloseDto?>: doc ghi rõ
        // "null nếu ngày đó chưa chốt" — annotation đúng hợp đồng.
        Task<ShiftCloseDto?> GetByDateAsync(DateTime date);

        /// <summary>
        /// Chốt ca: ghi số tiền mặt thực thu, hệ thống tự tính số tiền lẽ ra
        /// phải thu (tổng COD đơn hoàn tất trong ngày) và chênh lệch.
        /// Gọi lại cùng ngày chỉ cập nhật bản ghi cũ (không tạo trùng).
        /// </summary>
        /// <param name="input">Ngày, số tiền thực thu, ghi chú.</param>
        /// <returns>Bản chốt ca sau khi lưu.</returns>
        Task<ShiftCloseDto> CloseAsync(CloseShiftInput input);

        /// <summary>
        /// Lấy lịch sử chốt ca, mới nhất trước.
        /// </summary>
        /// <param name="take">Số bản ghi lấy, tối đa <see cref="MaxHistoryRows"/> (mặc định <see cref="DefaultHistoryRows"/>).</param>
        Task<List<ShiftCloseDto>> GetListAsync(int take = DefaultHistoryRows);
    }

    /// <summary>Item 129: COD end-of-shift reconciliation.</summary>
    [Authorize(Roles = RoleGroups.AdminCashier)]
    public class ShiftCloseAppService : ApplicationService, IShiftCloseAppService
    {
        private readonly IRepository<ShiftClose, Guid> _closes;
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>
        /// Khởi tạo service chốt ca (Admin/Cashier): đối chiếu đơn của ca với
        /// tiền thực thu để đóng ca.
        /// </summary>
        public ShiftCloseAppService(
            IRepository<ShiftClose, Guid> closes,
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _closes = closes;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Tính số tiền mặt lẽ ra phải thu trong ngày (xem doc ở interface):
        /// tổng các đơn COD đã hoàn tất trong ngày VN. Đơn hủy/bank/MoMo
        /// không vào vì không thu tiền mặt tại quầy.
        /// </summary>
        public virtual async Task<decimal> GetExpectedCodAsync(DateTime date)
        {
            var day = date.Date;
            // Biên ngày giờ VN → UTC (offset lấy từ VietnamTime, không hardcode 7).
            var startUtc = VietnamTime.DayStartUtc(day);
            var endUtc = VietnamTime.DayEndExclusiveUtc(day);

            var queryable = await _orders.GetQueryableAsync();

            // perf-hunt 2026-10-19: tính tổng ngay trên SQL (SUM) thay vì nạp
            // toàn bộ đơn COD của ngày về memory rồi Sum in-memory. Kết quả
            // giống hệt (WHERE giữ nguyên, SUM trên SQL Server dịch
            // được Money.Amount như các endpoint dashboard đã làm).
            return await _asyncExecuter.SumAsync(
                queryable.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc
                    && x.Status == OrderStatus.Completed
                    && x.PaymentMethod == PaymentMethod.Cod)
                .Select(x => x.TotalAmount.Amount));
        }

        /// <summary>
        /// Lấy bản chốt ca của 1 ngày (xem doc ở interface).
        /// </summary>
        public virtual async Task<ShiftCloseDto?> GetByDateAsync(DateTime date)
        {
            var day = date.Date;
            var queryable = await _closes.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Date == day));
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Chốt ca cuối ngày (xem doc ở interface). Lũy tiến: nhân viên có thể
        /// bấm chốt nhiều lần trong ca (đếm tiền từng đợt) nên tìm bản ghi
        /// của ngày trước — đã chốt rồi thì cập nhật, chưa có mới tạo.
        /// </summary>
        public virtual async Task<ShiftCloseDto> CloseAsync(CloseShiftInput input)
        {
            var day = input.Date.Date;
            var expected = await GetExpectedCodAsync(day);

            // bug-hunt 2026-10-20 (run 03:15): chống race check-then-insert —
            // 2 người bấm chốt cùng ngày đồng thời đều qua được FirstOrDefault
            // rồi cùng InsertAsync → cột Date có unique index → bên thua dính
            // DbUpdateException → 500. Serialize theo ngày bằng KeyedAsyncLock
            // (pattern như birthday-gift ở CustomerProfileAppService) thay vì
            // catch unique-violation vì cùng ngày cũng không được phép insert
            // trùng ngay cả ở cấp application.
            return await KeyedAsyncLock.RunAsync($"shift-close:{day:yyyy-MM-dd}", async () =>
            {
                var queryable = await _closes.GetQueryableAsync();
                var existing = await _asyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.Date == day));

                if (existing == null)
                {
                    existing = new ShiftClose
                    {
                        Date = day,
                        CreatedBy = CurrentUser?.Email ?? CurrentUser?.UserName ?? SystemActor.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _closes.InsertAsync(existing);
                }

                existing.ExpectedCod = expected;
                // Tiền thực thu không thể âm (nhập tay ở form admin) — ép về 0
                // để chênh lệch (Actual - Expected) luôn có nghĩa: âm = thiếu,
                // dương = thừa.
                existing.ActualCod = Math.Max(0, input.ActualCod);
                existing.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
                await _closes.UpdateAsync(existing, autoSave: true);

                return MapToDto(existing);
            });
        }

        /// <summary>
        /// Lấy lịch sử chốt ca, mới nhất trước (xem doc ở interface).
        /// </summary>
        public virtual async Task<List<ShiftCloseDto>> GetListAsync(int take = IShiftCloseAppService.DefaultHistoryRows)
        {
            var queryable = await _closes.GetQueryableAsync();
            // Chặn take âm/quá lớn để 1 request không kéo hết lịch sử.
            var list = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.Date).Take(Math.Clamp(take, 1, IShiftCloseAppService.MaxHistoryRows)));
            return list.Select(MapToDto).ToList();
        }

        private static ShiftCloseDto MapToDto(ShiftClose x)
        {
            return new ShiftCloseDto
            {
                Id = x.Id,
                Date = x.Date,
                ExpectedCod = x.ExpectedCod,
                ActualCod = x.ActualCod,
                Discrepancy = x.ActualCod - x.ExpectedCod,
                Note = x.Note,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt
            };
        }
    }
}
