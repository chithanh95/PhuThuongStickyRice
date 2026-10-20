using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    // ---------- Check-in (item 121) ----------

    /// <summary>
    /// Kết quả trả về khi khách điểm danh nhận thưởng (item 121): điểm được cộng,
    /// chuỗi ngày điểm danh liên tục và tổng điểm loyalty hiện tại của khách.
    /// </summary>
    public class CheckInResultModel
    {
        public bool AlreadyCheckedIn { get; set; }
        public int PointsAwarded { get; set; }
        public int StreakDays { get; set; }
        public int LoyaltyPoints { get; set; }
    }

    // ---------- Corporate clients (item 122) ----------

    /// <summary>
    /// Thông tin khách hàng doanh nghiệp đặt xôi theo lịch cố định hàng tuần (item 122).
    /// Weekday là thứ trong tuần giao hàng, ItemsJson chứa danh sách món đặt của công ty.
    /// </summary>
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

    /// <summary>
    /// Ảnh do khách hàng đăng để quán duyệt hiển thị trên web (item 123).
    /// </summary>
    public class CustomerPhotoModel
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public string Name { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------- Ingredient forecast (item 124) ----------

    /// <summary>
    /// Dự báo nguyên liệu cho một ngày (item 124): trung bình số phần bán ra, lượng nếp cần (kg)
    /// và định mức nếp mỗi phần (gram), cùng ghi chú heuristic dùng để tính.
    /// </summary>
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

    /// <summary>
    /// Ca làm việc của nhân viên trong một ngày, gồm giờ bắt đầu và giờ kết thúc (item 125).
    /// </summary>
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

    /// <summary>
    /// Tài khoản ví tiền của khách hàng, định danh bằng số điện thoại (item 140).
    /// </summary>
    public class WalletAccountModel
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public decimal Balance { get; set; }
    }

    /// <summary>
    /// Một giao dịch cộng hoặc trừ tiền ví khách hàng (item 140).
    /// BalanceAfter là số dư còn lại sau giao dịch, dùng để kiểm tra tính đúng đắn của lịch sử ví.
    /// </summary>
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

    /// <summary>
    /// Phiếu chốt ca (item 129): đối chiếu tiền COD hệ thống kỳ vọng với tiền thu thực tế,
    /// chênh lệch dương tức là thiếu hụt cần nhân viên giải trình.
    /// </summary>
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

    /// <summary>
    /// Mẫu tin nhắn dùng để gửi thông báo cho khách qua Zalo/Facebook (item 131).
    /// </summary>
    public class MessageTemplateModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Channel { get; set; }
        public string Body { get; set; }
    }

    // ---------- Voucher campaign (item 132) ----------

    /// <summary>
    /// Một nhóm đối tượng nhận voucher trong chiến dịch phát mã hàng loạt (item 132),
    /// ví dụ: khách đã mua trên 5 đơn, khách chưa mua 30 ngày.
    /// </summary>
    public class VoucherCampaignAudienceModel
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int RecipientCount { get; set; }
    }

    /// <summary>
    /// Mã voucher vừa được sinh và gắn với số điện thoại của khách nhận (item 132).
    /// </summary>
    public class GeneratedVoucherModel
    {
        public string Phone { get; set; }
        public string Code { get; set; }
    }

    // ---------- Dashboard widgets (items 122/124/133/139) ----------

    /// <summary>
    /// Tiến độ doanh thu hôm nay so với mục tiêu ngày đã đặt (items 122/124/133/139).
    /// </summary>
    public class DailyTargetModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal Target { get; set; }
        public double Percent { get; set; }
    }

    /// <summary>
    /// Thống kê số đơn và doanh thu theo kênh bán hàng (web, Zalo, điện thoại...),
    /// dùng trên dashboard để biết kênh nào hiệu quả nhất.
    /// </summary>
    public class SourceStatModel
    {
        public string Source { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    // ---------- Wave 6d (item 36): today vs yesterday ----------

    /// <summary>
    /// So sánh doanh thu và số đơn hôm nay với hôm qua (item 36):
    /// phần trăm tăng giảm dùng để hiển thị mũi tên lên/xuống trên dashboard.
    /// </summary>
    public class TodayVsYesterdayModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal YesterdayRevenue { get; set; }
        public long TodayOrders { get; set; }
        public long YesterdayOrders { get; set; }
        public double RevenueDeltaPercent { get; set; }
        public double OrdersDeltaPercent { get; set; }
    }

    // ---------- MoMo reconciliation (item 128) ----------

    /// <summary>
    /// Một đơn hàng trong màn hình đối chiếu MoMo (item 128):
    /// MoMoMatched cho biết đơn đã khớp với giao dịch MoMo thật hay chưa.
    /// </summary>
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

    /// <summary>
    /// Một dòng trong báo cáo hoàn tiền đơn hàng (item 130):
    /// trạng thái hoàn tiền và số tiền thực tế đã hoàn cho khách.
    /// </summary>
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
