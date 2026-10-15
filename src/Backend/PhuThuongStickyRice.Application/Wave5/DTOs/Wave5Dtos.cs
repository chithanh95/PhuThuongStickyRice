using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Wave5.DTOs
{
    // ---------- Check-in (item 121) ----------

    public class CheckInResultDto
    {
        /// <summary>True when the phone already checked in today (no new points).</summary>
        public bool AlreadyCheckedIn { get; set; }

        /// <summary>Points awarded by this check-in (0 when already checked in).</summary>
        public int PointsAwarded { get; set; }

        /// <summary>Current consecutive-day streak including today.</summary>
        public int StreakDays { get; set; }

        public int LoyaltyPoints { get; set; }
    }

    // ---------- Corporate clients (item 122) ----------

    public class CorporateClientDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public int Weekday { get; set; }
        public string WeekdayName { get; set; }
        public string ItemsJson { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUpdateCorporateClientDto
    {
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; }

        [StringLength(100)]
        public string ContactName { get; set; }

        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        /// <summary>0 = Sunday … 6 = Saturday.</summary>
        [Range(0, 6)]
        public int Weekday { get; set; }

        public string ItemsJson { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ---------- Customer photos (item 123) ----------

    public class CustomerPhotoDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public string Name { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCustomerPhotoDto
    {
        [StringLength(30)]
        public string Phone { get; set; }

        [StringLength(300)]
        public string Caption { get; set; }

        [StringLength(100)]
        public string Name { get; set; }
    }

    // ---------- Ingredient forecast (item 124) ----------

    public class IngredientForecastDto
    {
        /// <summary>The date being forecast (tomorrow morning, Vietnam date).</summary>
        public DateTime Date { get; set; }

        public string WeekdayName { get; set; }

        /// <summary>Average portions sold on the same weekday over the last 4 weeks.</summary>
        public double AveragePortions { get; set; }

        /// <summary>Estimated nếp needed (kg).</summary>
        public double NepKg { get; set; }

        public int NepPerPortionGrams { get; set; }

        /// <summary>Human-readable description of the heuristic.</summary>
        public string Heuristic { get; set; }
    }

    // ---------- Work shifts (item 125) ----------

    public class WorkShiftDto
    {
        public Guid Id { get; set; }
        public string StaffName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Note { get; set; }
    }

    public class CreateUpdateWorkShiftDto
    {
        [Required]
        [StringLength(100)]
        public string StaffName { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        [StringLength(500)]
        public string Note { get; set; }
    }

    // ---------- Wallet (item 140) ----------

    public class WalletAccountDto
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public decimal Balance { get; set; }
    }

    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public decimal BalanceAfter { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdjustWalletInput
    {
        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        /// <summary>Positive = top-up, negative = manual deduct.</summary>
        [Range(1000, 100000000)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }
    }

    // ---------- Shift close (item 129) ----------

    public class ShiftCloseDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal ExpectedCod { get; set; }
        public decimal ActualCod { get; set; }

        /// <summary>ActualCod - ExpectedCod.</summary>
        public decimal Discrepancy { get; set; }

        public string Note { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CloseShiftInput
    {
        public DateTime Date { get; set; }

        [Range(0, 1000000000)]
        public decimal ActualCod { get; set; }

        [StringLength(500)]
        public string Note { get; set; }
    }

    // ---------- Refunds (item 130) ----------

    public class RequestRefundInput
    {
        [Range(1000, 100000000)]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }
    }

    // ---------- Message templates (item 131) ----------

    public class MessageTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Channel { get; set; }
        public string Body { get; set; }
    }

    public class CreateUpdateMessageTemplateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>"SMS" or "Zalo".</summary>
        [Required]
        [StringLength(20)]
        public string Channel { get; set; }

        [Required]
        [StringLength(2000)]
        public string Body { get; set; }
    }

    // ---------- Voucher campaign (item 132) ----------

    public class VoucherCampaignAudienceDto
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int RecipientCount { get; set; }
    }

    public class CreateVoucherCampaignInput
    {
        /// <summary>"inactive30", "tier", or "all".</summary>
        [Required]
        public string Audience { get; set; }

        /// <summary>Only for audience "tier": "Bạc" or "Vàng".</summary>
        public string Tier { get; set; }

        /// <summary>Percent discount, or fixed amount when DiscountAmount is set.</summary>
        [Range(1, 100)]
        public decimal DiscountPercent { get; set; }

        /// <summary>Fixed VND amount (used instead of percent when > 0).</summary>
        [Range(0, 10000000)]
        public decimal DiscountAmount { get; set; }

        /// <summary>Days until expiry. Default 30.</summary>
        [Range(1, 365)]
        public int ValidDays { get; set; } = 30;

        [StringLength(50)]
        public string Prefix { get; set; } = "VC";
    }

    public class GeneratedVoucherDto
    {
        public string Phone { get; set; }
        public string Code { get; set; }
    }

    // ---------- POS (item 127) ----------

    public class PosOrderLineDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public Guid? SizeOptionId { get; set; }
        public List<Guid> ToppingIds { get; set; } = new List<Guid>();
    }

    public class CreatePosOrderInput
    {
        [StringLength(200)]
        public string CustomerName { get; set; }

        [StringLength(30)]
        public string CustomerPhone { get; set; }

        [Required]
        [MinLength(1)]
        public List<PosOrderLineDto> Lines { get; set; } = new List<PosOrderLineDto>();

        [StringLength(500)]
        public string Note { get; set; }
    }

    // ---------- Dashboard widgets (items 122/124/133/139) ----------

    public class DailyTargetDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal Target { get; set; }

        /// <summary>0-100+ percent.</summary>
        public double Percent { get; set; }
    }

    public class SourceStatDto
    {
        public string Source { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    // ---------- Wave 6d (item 36): today vs yesterday ----------

    public class TodayVsYesterdayDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal YesterdayRevenue { get; set; }
        public long TodayOrders { get; set; }
        public long YesterdayOrders { get; set; }

        /// <summary>Revenue delta %, signed. Null-safe: 100 when yesterday = 0 and today > 0.</summary>
        public double RevenueDeltaPercent { get; set; }

        /// <summary>Order-count delta %, signed.</summary>
        public double OrdersDeltaPercent { get; set; }
    }

    // ---------- MoMo reconciliation (item 128) ----------

    public class MoMoOrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public bool MoMoMatched { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
