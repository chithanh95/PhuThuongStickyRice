using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    // ---- Shippers (item 82) ----

    public class ShipperModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // ---- Blacklist (item 88) ----

    public class BlacklistedPhoneModel
    {
        public Guid Id { get; set; }

        public string Phone { get; set; }

        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // ---- Customer notes (item 89) ----

    public class CustomerNoteModel
    {
        public Guid Id { get; set; }

        public string Phone { get; set; }

        public string Note { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // ---- Promotions (item 91) ----

    public class PromotionModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal DiscountPercent { get; set; }

        /// <summary>"HH:mm".</summary>
        public string StartTime { get; set; }

        /// <summary>"HH:mm".</summary>
        public string EndTime { get; set; }

        /// <summary>Bit flags: 1=Mon .. 64=Sun, 127=all.</summary>
        public int DaysOfWeekFlags { get; set; }

        public bool IsActive { get; set; }
    }

    // ---- Reward rules (item 92) ----

    public class OrderRewardRuleModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal MinAmount { get; set; }

        /// <summary>"FreeTopping" or "DiscountCode".</summary>
        public string RewardType { get; set; }

        public string ToppingName { get; set; }

        public decimal? DiscountPercent { get; set; }

        public bool IsActive { get; set; }
    }

    // ---- Articles (item 93) ----

    public class ArticleModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string ImageUrl { get; set; }

        public DateTime PublishedAt { get; set; }

        public bool IsPublished { get; set; }
    }

    // ---- Table QR (item 95) ----

    public class TableQrModel
    {
        public Guid Id { get; set; }

        public string Label { get; set; }

        public string EncodedUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class TableQrGeneratedModel : TableQrModel
    {
        public string PngBase64 { get; set; }
    }

    // ---- Profit report (item 84) ----

    public class ProfitDayModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }

        public long Orders { get; set; }
    }

    public class ProfitProductModel
    {
        public string ProductName { get; set; }

        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }
    }

    public class ProfitReportModel
    {
        public decimal TotalRevenue { get; set; }

        public decimal TotalCost { get; set; }

        public decimal TotalProfit { get; set; }

        public List<ProfitDayModel> ByDay { get; set; } = new List<ProfitDayModel>();

        public List<ProfitProductModel> ByProduct { get; set; } = new List<ProfitProductModel>();
    }

    // ---- Login audit (item 99) ----

    public class LoginAuditModel
    {
        public Guid Id { get; set; }

        public string UserName { get; set; }

        public DateTime At { get; set; }

        public string IpAddress { get; set; }

        public bool Success { get; set; }

        public string FailureReason { get; set; }
    }

    public class PagedLoginAuditModel
    {
        public long TotalCount { get; set; }

        public List<LoginAuditModel> Items { get; set; } = new List<LoginAuditModel>();
    }

    // ---- 2FA (item 98) ----

    public class TwoFactorSetupModel
    {
        public string Secret { get; set; }

        public string OtpAuthUri { get; set; }

        public string QrDataUrl { get; set; }

        public bool IsEnabled { get; set; }
    }

    // ---- New vs returning (item 90) ----

    public class NewReturningCustomersModel
    {
        public long NewCustomers { get; set; }

        public long ReturningCustomers { get; set; }

        public long NewCustomerOrders { get; set; }

        public long ReturningCustomerOrders { get; set; }
    }

    // ---- Public reviews (item 87) ----

    public class PublicReviewModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        public string AdminReply { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
