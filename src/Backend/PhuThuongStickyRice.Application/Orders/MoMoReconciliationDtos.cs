using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Dòng đơn trong báo cáo đối soát MoMo: chỉ các đơn đã thanh toán qua MoMo,
    /// kèm cờ <see cref="MoMoMatched"/> để kế toán biết đơn nào đã khớp với callback
    /// MoMo, đơn nào cần đối soát tay.
    /// </summary>
    public class MoMoOrderDto
    {
        /// <summary>Id đơn hàng.</summary>
        public Guid Id { get; set; }

        /// <summary>Mã đơn (PX-...), dùng để khớp với OrderInfo trong callback MoMo.</summary>
        public string OrderNumber { get; set; }

        /// <summary>Tên khách hàng.</summary>
        public string CustomerName { get; set; }

        /// <summary>SĐT khách hàng.</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Tổng tiền đơn (VNĐ).</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Trạng thái thanh toán (tên enum PaymentStatus).</summary>
        public string PaymentStatus { get; set; }

        /// <summary>True khi callback MoMo đã tự đánh dấu đơn khớp với OrderInfo chứa mã đơn.</summary>
        public bool MoMoMatched { get; set; }

        /// <summary>Thời điểm tạo đơn.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
