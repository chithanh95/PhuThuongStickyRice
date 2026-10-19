using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Gói "Ăn sáng cả tuần" (item 185): gói T2–T6, mỗi ngày 1 món
    /// do quán xếp sẵn. Admin tạo gói tuần ở /admin/goi-tuan.
    /// </summary>
    public class WeeklyPlan : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Ngày thứ 2 đầu tuần áp dụng (date).</summary>
        public DateTime WeekStart { get; set; }

        /// <summary>JSON: [{"day":"Mon","productId":"...","productName":"...","price":25000}, ...] 5 ngày T2–T6.</summary>
        public string DayMenusJson { get; set; }

        /// <summary>Giá gói = tổng 5 món − giảm 10%.</summary>
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Đơn mua gói tuần của khách.
    /// </summary>
    public class WeeklyPlanOrder : Entity<Guid>, IAggregateRoot
    {
        public const int CustomerPhoneMaxLength = 30;

        public Guid WeeklyPlanId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public decimal PaidAmount { get; set; }

        /// <summary>Mã đơn để khách đưa quán nhận món mỗi ngày.</summary>
        public string OrderCode { get; set; }

        /// <summary>JSON: {"Mon":false,"Tue":true,...} — ngày nào đã nhận.</summary>
        public string ClaimedDaysJson { get; set; }

        public WeeklyPlanOrderStatus Status { get; set; } = WeeklyPlanOrderStatus.Paid;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum WeeklyPlanOrderStatus
    {
        Paid = 0,
        Completed = 1,
        Cancelled = 2
    }
}
