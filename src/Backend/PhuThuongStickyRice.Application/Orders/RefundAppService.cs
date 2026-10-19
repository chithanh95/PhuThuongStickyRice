
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

    public class RefundReportRowDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public string RefundStatus { get; set; }
        public decimal RefundAmount { get; set; }
        public string PaymentMethod { get; set; }
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
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IPermissionService _permissions;
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
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.RefundStatus != RefundStatus.None)
                    .OrderByDescending(x => x.CreatedAt));

            return list.Select(x => new RefundReportRowDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerName = x.CustomerName,
                CustomerPhone = x.CustomerPhone,
                TotalAmount = x.TotalAmount.Amount,
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
            await _activityLogger.LogAsync(StaffActions.OrderRefundRequest,
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
            await _activityLogger.LogAsync(StaffActions.OrderRefundConfirm,
                $"Xác nhận hoàn tiền đơn {order.OrderNumber}.");
        }
    }
}
