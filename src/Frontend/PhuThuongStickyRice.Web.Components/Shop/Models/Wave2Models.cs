using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>Loyalty account summary (public + admin).</summary>
    public class LoyaltyAccountModel
    {
        public string Phone { get; set; }

        public int Points { get; set; }

        public long TotalEarned { get; set; }

        public decimal TotalSpent { get; set; }

        public string ReferralCode { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>Đồng / Bạc / Vàng.</summary>
        public string Tier { get; set; }

        public decimal TierDiscountPercent { get; set; }
    }

    public class AdjustPointsModel
    {
        public int PointsDelta { get; set; }

        public string Reason { get; set; }
    }

    /// <summary>Result of the post-checkout lucky spin.</summary>
    public class SpinResultModel
    {
        public string Segment { get; set; }

        public string PrizeLabel { get; set; }

        public string Code { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    public class SupportTicketModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public string OrderNumber { get; set; }

        public string Message { get; set; }

        public string Status { get; set; }

        public string ReplyNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RepliedAt { get; set; }
    }

    public class CreateSupportTicketModel
    {
        public string Name { get; set; }

        public string Phone { get; set; }

        public string OrderNumber { get; set; }

        public string Message { get; set; }
    }

    public class FaqItemModel
    {
        public Guid Id { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class SubscriptionModel
    {
        public Guid Id { get; set; }

        public string Phone { get; set; }

        public string CustomerName { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string SizeName { get; set; }

        public string ToppingNames { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public string DeliveryAddress { get; set; }

        public DateTime StartDate { get; set; }

        public bool Active { get; set; }

        public string Notes { get; set; }

        public DateTime? LastOrderDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateSubscriptionModel
    {
        public string CustomerName { get; set; }

        public string Phone { get; set; }

        public Guid ProductId { get; set; }

        public Guid? SizeOptionId { get; set; }

        public List<Guid> ToppingIds { get; set; } = new List<Guid>();

        public int Quantity { get; set; } = 1;

        public string DeliveryAddress { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);

        public string Notes { get; set; }
    }

    public class OrderReviewModel
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        public bool IsHidden { get; set; }

        public string AdminReply { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Wave 6b (item 26): per-dish star ratings.</summary>
        public List<OrderReviewLineModel> Lines { get; set; } = new List<OrderReviewLineModel>();

        /// <summary>Item 44: photo attached to the review.</summary>
        public string PhotoUrl { get; set; }

        /// <summary>Item 44: auto-generated reward voucher code (free xoi) for photo reviews.</summary>
        public string RewardCode { get; set; }
    }

    /// <summary>Wave 6b (item 26): per-dish star rating inside a review.</summary>
    public class OrderReviewLineModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int Rating { get; set; }
    }

    public class ZaloStatusModel
    {
        public bool Configured { get; set; }

        public string OaId { get; set; }

        public string AccessToken { get; set; }

        public string Note { get; set; }
    }
}
