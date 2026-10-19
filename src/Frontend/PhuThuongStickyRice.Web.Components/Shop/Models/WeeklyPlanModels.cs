using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>Món của 1 ngày trong gói tuần (Mon..Fri).</summary>
    public class WeeklyPlanDayModel
    {
        public string DayOfWeek { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Price { get; set; }
    }

    /// <summary>Gói "Ăn sáng cả tuần".</summary>
    public class WeeklyPlanModel
    {
        public Guid Id { get; set; }

        public DateTime WeekStart { get; set; }

        public List<WeeklyPlanDayModel> Days { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public int BuyerCount { get; set; }
    }

    /// <summary>Đơn mua gói tuần của khách.</summary>
    public class WeeklyPlanOrderModel
    {
        public Guid Id { get; set; }

        public string OrderCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public decimal PaidAmount { get; set; }

        public Dictionary<string, bool> ClaimedDays { get; set; }

        /// <summary>Paid / Completed / Cancelled.</summary>
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
