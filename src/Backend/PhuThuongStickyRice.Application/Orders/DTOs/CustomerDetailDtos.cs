using PhuThuongStickyRice.Application.Customer;
using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// Item 168: detailed purchase history of one customer (looked up by phone).
    /// Read-only — aggregates the customer's orders; no schema changes.
    /// </summary>
    public class CustomerDetailDto
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Orders excluding cancelled ones.</summary>
        public long TotalOrders { get; set; }

        public long CancelledOrders { get; set; }

        /// <summary>Lifetime spend, excluding cancelled orders.</summary>
        public decimal TotalSpent { get; set; }

        public DateTime? FirstOrderAt { get; set; }

        public DateTime? LastOrderAt { get; set; }

        public List<CustomerTopProductDto> TopProducts { get; set; }
            = new List<CustomerTopProductDto>();

        public List<CustomerRecentOrderDto> RecentOrders { get; set; }
            = new List<CustomerRecentOrderDto>();

        /// <summary>Item 149 tags attached to this customer phone.</summary>
        public List<CustomerTagDto> Tags { get; set; }
            = new List<CustomerTagDto>();
    }

    /// <summary>
    /// Món bán chạy nhất của 1 khách (xếp theo tổng số lượng đã mua), dùng
    /// trong trang chi tiết khách hàng để nhân viên tư vấn đúng món quen.
    /// </summary>
    public class CustomerTopProductDto
    {
        public string ProductName { get; set; }

        public long Quantity { get; set; }
    }

    /// <summary>
    /// Tóm tắt 1 đơn gần nhất của khách trong trang chi tiết khách hàng:
    /// mã đơn, thời gian đặt, tổng tiền, trạng thái và phương thức thanh toán.
    /// </summary>
    public class CustomerRecentOrderDto
    {
        public string OrderNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }

        public string StatusLabel { get; set; }

        public string PaymentMethodLabel { get; set; }
    }
}
