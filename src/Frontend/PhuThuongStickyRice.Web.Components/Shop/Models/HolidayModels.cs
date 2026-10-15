using System;
using System.Text.Json.Serialization;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>Item 117: lịch nghỉ lễ/Tết của quán.</summary>
    public class HolidayModel
    {
        public Guid Id { get; set; }

        public DateTime HolidayDate { get; set; }

        public string Reason { get; set; }

        public bool IsFullDay { get; set; }
    }

    public class CreateHolidayModel
    {
        /// <summary>Binds to &lt;input type="date"&gt;; only the date part is used.</summary>
        public DateTime HolidayDate { get; set; } = DateTime.Today;

        public string Reason { get; set; }

        public bool IsFullDay { get; set; } = true;
    }
}
