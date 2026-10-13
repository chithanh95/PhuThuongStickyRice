using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    public class OrderLineDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }

    public class OrderDto
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        public string Status { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();
    }

    public class DashboardStatsDto
    {
        public long TotalOrders { get; set; }

        public long PendingOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public long TotalProductsSold { get; set; }

        public long TotalProducts { get; set; }

        public List<OrdersByStatusDto> OrdersByStatus { get; set; } = new List<OrdersByStatusDto>();

        public List<RevenueByDayDto> RevenueByDay { get; set; } = new List<RevenueByDayDto>();

        public List<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
    }

    public class OrdersByStatusDto
    {
        public string Status { get; set; }

        public long Count { get; set; }
    }

    public class RevenueByDayDto
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public long Orders { get; set; }
    }
}
