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
        /// <summary>Độ dài tối đa cột SĐT trong DB.</summary>
        public const int CustomerPhoneMaxLength = 30;

        /// <summary>Gói tuần mà đơn này mua.</summary>
        public Guid WeeklyPlanId { get; set; }

        /// <summary>Tên khách mua gói.</summary>
        public string CustomerName { get; set; }

        /// <summary>SĐT khách — dùng để xác thực khi khách đến nhận món mỗi ngày.</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Số tiền khách đã trả cho gói (VND).</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>Mã đơn để khách đưa quán nhận món mỗi ngày.</summary>
        public string OrderCode { get; set; }

        /// <summary>JSON: {"Mon":false,"Tue":true,...} — ngày nào đã nhận.</summary>
        public string ClaimedDaysJson { get; set; }

        /// <summary>Trạng thái đơn mua gói (Paid = đang dùng, Completed = đã nhận đủ 5 ngày, Cancelled = đã hủy).</summary>
        public WeeklyPlanOrderStatus Status { get; set; } = WeeklyPlanOrderStatus.Paid;

        /// <summary>Thời điểm mua gói (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Trạng thái đơn mua gói tuần: Paid (đang trong tuần sử dụng),
    /// Completed (đã nhận đủ 5 ngày), Cancelled (đã hủy).
    /// </summary>
    public enum WeeklyPlanOrderStatus
    {
        Paid = 0,
        Completed = 1,
        Cancelled = 2
    }
}
