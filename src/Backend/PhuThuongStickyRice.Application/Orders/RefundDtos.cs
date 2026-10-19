using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Đầu vào ghi nhận yêu cầu hoàn tiền cho một đơn (admin).
    /// Service này chỉ lưu trạng thái yêu cầu/xác nhận để đối soát — tiền thực tế
    /// chuyển khoản tay cho khách, không tự động chuyển tiền.
    /// </summary>
    public class RequestRefundInput
    {
        /// <summary>
        /// Số tiền hoàn (VNĐ), từ 1.000đ đến 100.000.000đ.
        /// Không được vượt tổng tiền đơn — kiểm tra ở logic domain
        /// (<c>Order.RequestRefund</c>) vì cần so với <c>TotalAmount</c> thực tế.
        /// </summary>
        [Range(1000, 100000000)]
        public decimal Amount { get; set; }

        /// <summary>Lý do hoàn tiền, tối đa 500 ký tự, ghi vào lịch sử để truy vết.</summary>
        [StringLength(500)]
        public string Reason { get; set; }
    }
}
