using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Blazor.Modules.Admin.Models
{
    // ---------- Check-in (item 121) ----------

    public class CheckInResultModel
    {
        public bool AlreadyCheckedIn { get; set; }
        public int PointsAwarded { get; set; }
        public int StreakDays { get; set; }
        public int LoyaltyPoints { get; set; }
    }

    // ---------- Corporate clients (item 122) ----------

    public class CorporateClientModel
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

    // ---------- Customer photos (item 123) ----------

    public class CustomerPhotoModel
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------- Ingredient forecast (item 124) ----------

    public class IngredientForecastModel
    {
        public DateTime Date { get; set; }
        public string WeekdayName { get; set; }
        public double AveragePortions { get; set; }
        public double NepKg { get; set; }
        public int NepPerPortionGrams { get; set; }
        public string Heuristic { get; set; }
    }

    // ---------- Work shifts (item 125) ----------

    public class WorkShiftModel
    {
        public Guid Id { get; set; }
        public string StaffName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Note { get; set; }
    }

    // ---------- Wallet (item 140) ----------

    public class WalletAccountModel
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public decimal Balance { get; set; }
    }

    public class WalletTransactionModel
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public decimal BalanceAfter { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------- Shift close (item 129) ----------

    public class ShiftCloseModel
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal ExpectedCod { get; set; }
        public decimal ActualCod { get; set; }
        public decimal Discrepancy { get; set; }
        public string Note { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------- Message templates (item 131) ----------

    public class MessageTemplateModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Channel { get; set; }
        public string Body { get; set; }
    }

    // ---------- Voucher campaign (item 132) ----------

    public class VoucherCampaignAudienceModel
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int RecipientCount { get; set; }
    }

    public class GeneratedVoucherModel
    {
        public string Phone { get; set; }
        public string Code { get; set; }
    }

    // ---------- Dashboard widgets (items 122/124/133/139) ----------

    public class DailyTargetModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal Target { get; set; }
        public double Percent { get; set; }
    }

    public class SourceStatModel
    {
        public string Source { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    // ---------- MoMo reconciliation (item 128) ----------

    public class MoMoOrderModel
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

    // ---------- Refunds (item 130) ----------

    public class RefundReportRowModel
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public string RefundStatus { get; set; }
        public decimal RefundAmount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
