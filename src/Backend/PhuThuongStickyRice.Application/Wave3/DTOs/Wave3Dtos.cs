using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Wave3.DTOs
{
    // ---- Shippers (item 82) ----

    public class ShipperDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateShipperDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(30)]
        public string Phone { get; set; }
    }

    public class UpdateShipperDto : CreateShipperDto
    {
        public bool IsActive { get; set; } = true;
    }

    // ---- Blacklist (item 88) ----

    public class BlacklistedPhoneDto
    {
        public Guid Id { get; set; }

        public string Phone { get; set; }

        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateBlacklistedPhoneDto
    {
        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }
    }

    // ---- Customer notes (item 89) ----

    public class CustomerNoteDto
    {
        public Guid Id { get; set; }

        public string Phone { get; set; }

        public string Note { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateCustomerNoteDto
    {
        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        [Required]
        [StringLength(2000)]
        public string Note { get; set; }
    }

    // ---- Promotions / happy hour (item 91) ----

    public class PromotionDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal DiscountPercent { get; set; }

        /// <summary>Local start time "HH:mm".</summary>
        public string StartTime { get; set; }

        /// <summary>Local end time "HH:mm".</summary>
        public string EndTime { get; set; }

        /// <summary>Bit flags of PromotionDay (1=Mon .. 64=Sun, 127=all).</summary>
        public int DaysOfWeekFlags { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreatePromotionDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Range(0.01, 100)]
        public decimal DiscountPercent { get; set; }

        /// <summary>"HH:mm".</summary>
        [Required]
        public string StartTime { get; set; }

        /// <summary>"HH:mm".</summary>
        [Required]
        public string EndTime { get; set; }

        [Range(1, 127)]
        public int DaysOfWeekFlags { get; set; } = 127;

        public bool IsActive { get; set; } = true;
    }

    // ---- Reward rules (item 92) ----

    public class OrderRewardRuleDto
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

    public class CreateOrderRewardRuleDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinAmount { get; set; }

        [Required]
        public string RewardType { get; set; }

        [StringLength(200)]
        public string ToppingName { get; set; }

        [Range(0.01, 100)]
        public decimal? DiscountPercent { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ---- Articles (item 93) ----

    public class ArticleDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string ImageUrl { get; set; }

        public DateTime PublishedAt { get; set; }

        public bool IsPublished { get; set; }
    }

    public class CreateArticleDto
    {
        [Required]
        [StringLength(300)]
        public string Title { get; set; }

        [Required]
        [StringLength(10000)]
        public string Body { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        public DateTime? PublishedAt { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    // ---- Table QR (item 95) ----

    public class TableQrDto
    {
        public Guid Id { get; set; }

        public string Label { get; set; }

        public string EncodedUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateTableQrDto
    {
        [Required]
        [StringLength(100)]
        public string Label { get; set; }
    }

    public class TableQrGeneratedDto : TableQrDto
    {
        /// <summary>PNG bytes as base64 (data URL built client-side).</summary>
        public string PngBase64 { get; set; }
    }

    // ---- Profit report (item 84) ----

    public class ProfitDayDto
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }

        public long Orders { get; set; }
    }

    public class ProfitProductDto
    {
        public string ProductName { get; set; }

        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }
    }

    public class ProfitReportDto
    {
        public decimal TotalRevenue { get; set; }

        public decimal TotalCost { get; set; }

        public decimal TotalProfit { get; set; }

        public List<ProfitDayDto> ByDay { get; set; } = new List<ProfitDayDto>();

        public List<ProfitProductDto> ByProduct { get; set; } = new List<ProfitProductDto>();
    }

    // ---- Login audit (item 99) ----

    public class LoginAuditDto
    {
        public Guid Id { get; set; }

        public string UserName { get; set; }

        public DateTime At { get; set; }

        public string IpAddress { get; set; }

        public bool Success { get; set; }

        public string FailureReason { get; set; }
    }

    // ---- 2FA (item 98) ----

    public class TwoFactorSetupDto
    {
        /// <summary>Base32 secret; also embedded in the QR.</summary>
        public string Secret { get; set; }

        /// <summary>otpauth:// URI.</summary>
        public string OtpAuthUri { get; set; }

        /// <summary>PNG data URL of the QR code.</summary>
        public string QrDataUrl { get; set; }

        public bool IsEnabled { get; set; }
    }

    public class VerifyTwoFactorDto
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; }
    }

    public class EnableTwoFactorDto : VerifyTwoFactorDto
    {
        [Required]
        public string Secret { get; set; }
    }

    // ---- New vs returning customers (item 90) ----

    public class NewReturningCustomersDto
    {
        public long NewCustomers { get; set; }

        public long ReturningCustomers { get; set; }

        public long NewCustomerOrders { get; set; }

        public long ReturningCustomerOrders { get; set; }
    }

    // ---- Owner report (item 85) ----

    public class OwnerReportResultDto
    {
        public string Period { get; set; }

        public bool Sent { get; set; }

        public string Reason { get; set; }
    }
}
