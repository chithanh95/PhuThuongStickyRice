
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Orders;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IRefundAppService : IApplicationService
    {
        /// <summary>Admin: orders with a refund request or a completed refund.</summary>
        Task<List<RefundReportRowDto>> GetReportAsync();

        /// <summary>Admin: records a refund request (money movement stays manual).</summary>
        Task RequestRefundAsync(Guid orderId, RequestRefundInput input);

        /// <summary>Admin: marks the refund as paid out.</summary>
        Task ConfirmRefundAsync(Guid orderId);
    }

    /// <summary>
    /// Một dòng trong báo cáo hoàn tiền (đơn có yêu cầu hoàn hoặc đã hoàn xong).
    /// </summary>
    public class RefundReportRowDto
    {
        /// <summary>Id đơn hàng.</summary>
        public Guid Id { get; set; }

        /// <summary>Mã đơn hàng (vd: PX-20261019-4821).</summary>
        public string OrderNumber { get; set; }

        /// <summary>Tên khách hàng.</summary>
        public string CustomerName { get; set; }

        /// <summary>SĐT khách hàng.</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Tổng tiền đơn hàng (VNĐ).</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Trạng thái hoàn tiền (Requested/Refunded...).</summary>
        public string RefundStatus { get; set; }

        /// <summary>Số tiền yêu cầu hoàn (VNĐ).</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Phương thức thanh toán của đơn (vd: MoMo, COD).</summary>
        public string PaymentMethod { get; set; }

        /// <summary>Thời điểm tạo đơn.</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Item 130: theo dõi hoàn tiền cho đơn hàng (chỉ ghi nhận trên hệ thống).
    /// Tiền thực tế chuyển khoản tay cho khách — service này chỉ lưu trạng thái
    /// yêu cầu/xác nhận để kế toán đối soát, không tự động chuyển tiền.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class RefundAppService : ApplicationService, IRefundAppService
    {
        /// <summary>
        /// Số dòng tối đa của báo cáo hoàn tiền — bảng Orders tăng không giới hạn
        /// nên phải giới hạn server-side, không bao giờ materialize toàn bộ.
        /// </summary>
        private const int MaxReportRows = 500;

        /// <summary>Repository bảng Order (đơn hàng cần hoàn tiền).</summary>
        private readonly IRepository<Order, Guid> _orders;

        /// <summary>Thực thi query async trên IQueryable (ABP).</summary>
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Kiểm tra quyền nghiệp vụ (Item 155: quyền "orders.refund").</summary>
        private readonly IPermissionService _permissions;

        /// <summary>Ghi log hoạt động nhân viên (Item 156: truy vết ai hoàn tiền đơn nào).</summary>
        private readonly IStaffActivityLogger _activityLogger;

        /// <summary>Khởi tạo service hoàn tiền.</summary>
        public RefundAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
            _permissions = permissions;
            _activityLogger = activityLogger;
        }

        /// <summary>
        /// Admin: báo cáo các đơn có yêu cầu hoàn tiền hoặc đã hoàn tiền xong,
        /// sắp xếp đơn mới nhất trước.
        /// </summary>
        public virtual async Task<List<RefundReportRowDto>> GetReportAsync()
        {
            var queryable = await _orders.GetQueryableAsync();
            // perf-hunt 2026-10-19: Take(500) server-side — bảng Orders tăng
            // không giới hạn, không bao giờ materialize toàn bộ lịch sử hoàn
            // tiền. Có index IX_Orders_RefundStatus phục vụ filter này.
            // perf-hunt 2026-10-19 (run 17:55): project scalar ngay trong SQL thay
            // vì materialize full entity Order (tracked) — DTO chỉ dùng 9 cột.
            // Enum.ToString() không translate được nên map sau ToListAsync.
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.RefundStatus != RefundStatus.None)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(MaxReportRows)
                    .Select(x => new
                    {
                        x.Id,
                        x.OrderNumber,
                        x.CustomerName,
                        x.CustomerPhone,
                        TotalAmount = x.TotalAmount.Amount,
                        x.RefundStatus,
                        x.RefundAmount,
                        x.PaymentMethod,
                        x.CreatedAt
                    }));

            return list.Select(x => new RefundReportRowDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerName = x.CustomerName,
                CustomerPhone = x.CustomerPhone,
                TotalAmount = x.TotalAmount,
                RefundStatus = x.RefundStatus.ToString(),
                RefundAmount = x.RefundAmount,
                PaymentMethod = x.PaymentMethod.ToString(),
                CreatedAt = x.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Admin: ghi nhận yêu cầu hoàn tiền cho một đơn (kèm số tiền và lý do).
        /// Yêu cầu quyền riêng "orders.refund" (Item 155) và ghi log hoạt động nhân viên
        /// (Item 156) để truy vết ai yêu cầu hoàn tiền đơn nào.
        /// </summary>
        /// <param name="orderId">Id đơn hàng cần hoàn tiền.</param>
        /// <param name="input">Số tiền và lý do hoàn tiền.</param>
        public virtual async Task RequestRefundAsync(Guid orderId, RequestRefundInput input)
        {
            // Item 155: refunds need orders.refund.
            await _permissions.RequirePermissionAsync(PermissionKeys.OrdersRefund);

            var order = await _orders.GetAsync(orderId);
            order.RequestRefund(input.Amount, input.Reason);
            await _orders.UpdateAsync(order, autoSave: true);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActivityActions.OrderRefundRequest,
                $"Yêu cầu hoàn tiền đơn {order.OrderNumber}.");
        }

        /// <summary>
        /// Admin: xác nhận đã chuyển tiền hoàn cho khách (sau khi chuyển khoản tay).
        /// Yêu cầu quyền "orders.refund" và ghi log hoạt động nhân viên như khi yêu cầu.
        /// </summary>
        /// <param name="orderId">Id đơn hàng đã hoàn tiền.</param>
        public virtual async Task ConfirmRefundAsync(Guid orderId)
        {
            // Item 155: refunds need orders.refund.
            await _permissions.RequirePermissionAsync(PermissionKeys.OrdersRefund);

            var order = await _orders.GetAsync(orderId);
            order.ConfirmRefund();
            await _orders.UpdateAsync(order, autoSave: true);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActivityActions.OrderRefundConfirm,
                $"Xác nhận hoàn tiền đơn {order.OrderNumber}.");
        }
    }
}
