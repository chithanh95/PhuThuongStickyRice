using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.WeeklyPlans
{
    /// <summary>Một ngày trong gói tuần (T2..T6).</summary>
    public class WeeklyPlanDayDto
    {
        /// <summary>Mon..Fri.</summary>
        public string DayOfWeek { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Price { get; set; }
    }

    public class WeeklyPlanDto
    {
        public Guid Id { get; set; }

        /// <summary>Ngày thứ 2 đầu tuần.</summary>
        public DateTime WeekStart { get; set; }

        public List<WeeklyPlanDayDto> Days { get; set; }

        /// <summary>Giá gói (đã giảm 10%).</summary>
        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Số người đã mua (dùng cho trang admin).</summary>
        public int BuyerCount { get; set; }
    }

    public class WeeklyPlanDayInput
    {
        /// <summary>Mon..Fri.</summary>
        public string DayOfWeek { get; set; }

        public Guid ProductId { get; set; }
    }

    public class CreateWeeklyPlanDto
    {
        public DateTime WeekStart { get; set; }

        public List<WeeklyPlanDayInput> Days { get; set; }
    }

    public class BuyWeeklyPlanDto
    {
        public Guid WeeklyPlanId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }
    }

    public class WeeklyPlanOrderDto
    {
        public Guid Id { get; set; }

        /// <summary>Mã đơn dạng TUANxxxxxx.</summary>
        public string OrderCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public decimal PaidAmount { get; set; }

        /// <summary>{"Mon":false,...,"Fri":false}.</summary>
        public Dictionary<string, bool> ClaimedDays { get; set; }

        /// <summary>Paid / Completed / Cancelled.</summary>
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
