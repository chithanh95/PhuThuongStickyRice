using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Orders.Models
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

        public string SizeName { get; set; }

        public string ToppingNames { get; set; }

        /// <summary>Per-item note from the customer.</summary>
        public string Note { get; set; }
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

        /// <summary>
        /// Wave 6c (item 33): internal staff note. Admin endpoints only —
        /// never returned by public track-order endpoints.
        /// </summary>
        public string AdminNote { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        /// <summary>"ShipperPickup" / "Pickup" / legacy "Delivery".</summary>
        public string FulfillmentMethod { get; set; }

        /// <summary>Name of the customer-booked pickup shipper. Nullable.</summary>
        public string PickupShipperName { get; set; }

        /// <summary>Phone of the customer-booked pickup shipper. Nullable.</summary>
        public string PickupShipperPhone { get; set; }

        /// <summary>When the order was collected at the shop (UTC). Nullable.</summary>
        public DateTime? PickedUpAt { get; set; }

        /// <summary>Pre-order scheduled pickup time (UTC). Null = ASAP.</summary>
        public DateTime? ScheduledFor { get; set; }

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

        public Guid? ShipperId { get; set; }

        public string ShipperName { get; set; }

        public string PromotionName { get; set; }

        public decimal PromotionDiscountAmount { get; set; }

        /// <summary>Item 133: "Web" / "QR bàn" / "Link chia sẻ" / "Nhóm" / "POS".</summary>
        public string Source { get; set; }

        /// <summary>Item 128: MoMo IPN auto-matched by parsing OrderInfo.</summary>
        public bool MoMoMatched { get; set; }

        /// <summary>Item 130: "None" / "Requested" / "Refunded".</summary>
        public string RefundStatus { get; set; }

        /// <summary>Item 130: refund amount in VND.</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Wave 6a (item 23): scheduled delivery slot. Null = giao ngay.</summary>
        public string DeliverySlot { get; set; }

        public List<OrderLineModel> Lines { get; set; } = new List<OrderLineModel>();
    }

    public class UpdateOrderStatusModel
    {
        public string Status { get; set; }
    }

    /// <summary>Wave 6c (item 34): one recorded status transition.</summary>
    public class OrderStatusHistoryModel
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string FromStatus { get; set; }

        public string ToStatus { get; set; }

        public DateTime At { get; set; }

        public string ByUser { get; set; }
    }

    /// <summary>Wave 6c (item 31): one payment channel of the daily reconciliation.</summary>
    public class ReconciliationChannelModel
    {
        public string PaymentMethod { get; set; }

        public string Label { get; set; }

        public long OrderCount { get; set; }

        public decimal Revenue { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal UnpaidAmount { get; set; }
    }

    /// <summary>Wave 6c (item 31): daily payment-channel reconciliation.</summary>
    public class DailyReconciliationModel
    {
        public DateTime Date { get; set; }

        public List<ReconciliationChannelModel> Channels { get; set; } = new List<ReconciliationChannelModel>();

        public long TotalOrderCount { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal TotalPaidAmount { get; set; }

        public decimal TotalUnpaidAmount { get; set; }
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

        /// <summary>Item 86: Pending orders older than StalePendingMinutes.</summary>
        public long StalePendingOrders { get; set; }

        public int StalePendingMinutes { get; set; }

        public decimal TotalRevenue { get; set; }

        public long TotalProductsSold { get; set; }

        public long TotalProducts { get; set; }

        public List<OrdersByStatusModel> OrdersByStatus { get; set; } = new List<OrdersByStatusModel>();

        public List<RevenueByDayModel> RevenueByDay { get; set; } = new List<RevenueByDayModel>();

        public List<OrderModel> RecentOrders { get; set; } = new List<OrderModel>();

        /// <summary>Pre-orders sorted by scheduled time — kitchen priority.</summary>
        public List<OrderModel> UpcomingPreOrders { get; set; } = new List<OrderModel>();
    }

    /// <summary>Revenue forecast models (admin /admin/du-bao).</summary>
    public class ForecastHistoryPointModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public long OrderCount { get; set; }
        public long Portions { get; set; }
    }

    public class WeekdayPatternModel
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string WeekdayName { get; set; }
        public int Occurrences { get; set; }
        public decimal AvgRevenue { get; set; }
        public double AvgOrders { get; set; }
        public double AvgPortions { get; set; }
        public double RatioVsAverage { get; set; }
    }

    public class ForecastDayModel
    {
        public DateTime Date { get; set; }
        public string WeekdayName { get; set; }
        public decimal ForecastRevenue { get; set; }
        public double ForecastOrders { get; set; }
        public long SuggestedPortions { get; set; }
        public bool BasedOnWeekdayPattern { get; set; }
    }

    public class ForecastTotalModel
    {
        public decimal ForecastRevenue { get; set; }
        public double ForecastOrders { get; set; }
        public long SuggestedPortions { get; set; }
    }

    public class RevenueForecastModel
    {
        public DateTime GeneratedAt { get; set; }
        public int HistoryDays { get; set; }
        public int DaysWithOrders { get; set; }
        public decimal AvgDailyRevenue7d { get; set; }
        public decimal AvgDailyRevenue30d { get; set; }
        public List<ForecastHistoryPointModel> History { get; set; } = new List<ForecastHistoryPointModel>();
        public List<WeekdayPatternModel> WeekdayPattern { get; set; } = new List<WeekdayPatternModel>();
        public List<ForecastDayModel> Next7Days { get; set; } = new List<ForecastDayModel>();
        public ForecastTotalModel Next7DaysTotal { get; set; } = new ForecastTotalModel();
        public ForecastTotalModel Next30DaysTotal { get; set; } = new ForecastTotalModel();
        public string MethodNote { get; set; }
        public string Disclaimer { get; set; }
    }

    /// <summary>Item 99: hourly best-sellers report (heatmap source).</summary>
    public class HourlySalesRowModel
    {
        public int Hour { get; set; }
        public List<long> Quantities { get; set; } = new List<long>();
        public long Total { get; set; }
    }

    public class TopHourModel
    {
        public int Hour { get; set; }
        public long Portions { get; set; }
        public long Orders { get; set; }
    }

    public class HourlySalesReportModel
    {
        public DateTime Date { get; set; }
        public List<string> Products { get; set; } = new List<string>();
        public List<HourlySalesRowModel> Hours { get; set; } = new List<HourlySalesRowModel>();
        public List<TopHourModel> TopHours { get; set; } = new List<TopHourModel>();
        public long TotalOrders { get; set; }
        public long TotalPortions { get; set; }
        public string Suggestion { get; set; }
    }
}
