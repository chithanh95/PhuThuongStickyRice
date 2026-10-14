using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Blazor.Modules.Orders.Models
{
    public class PagedResult<T>
    {
        public long TotalCount { get; set; }

        public List<T> Items { get; set; } = new List<T>();
    }

    public class OrderLineModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }

    public class OrderModel
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public string DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal LoyaltyDiscount { get; set; }

        public int LoyaltyPointsRedeemed { get; set; }

        public decimal TierDiscount { get; set; }

        public string ReferralCodeUsed { get; set; }

        public string TaxCode { get; set; }

        public string CompanyName { get; set; }

        public bool RequiresDeposit { get; set; }

        public string RecipientName { get; set; }

        public string RecipientPhone { get; set; }

        public bool IsGift { get; set; }

        public string GiftMessage { get; set; }

        public decimal GiftFee { get; set; }

        public List<OrderLineModel> Lines { get; set; } = new List<OrderLineModel>();
    }

    public class UpdateOrderStatusModel
    {
        public string Status { get; set; }
    }

    public class OrdersByStatusModel
    {
        public string Status { get; set; }

        public long Count { get; set; }
    }

    public class RevenueByDayModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public long Orders { get; set; }
    }

    public class DashboardStatsModel
    {
        public long TotalOrders { get; set; }

        public long PendingOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public long TotalProductsSold { get; set; }

        public long TotalProducts { get; set; }

        public List<OrdersByStatusModel> OrdersByStatus { get; set; } = new List<OrdersByStatusModel>();

        public List<RevenueByDayModel> RevenueByDay { get; set; } = new List<RevenueByDayModel>();

        public List<OrderModel> RecentOrders { get; set; } = new List<OrderModel>();
    }
}
