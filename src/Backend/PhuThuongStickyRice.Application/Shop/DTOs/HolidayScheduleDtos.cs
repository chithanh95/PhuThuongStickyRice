using System;

namespace PhuThuongStickyRice.Application.Shop.DTOs
{
    public class HolidayScheduleDto
    {
        public Guid Id { get; set; }

        /// <summary>The holiday date (date part only).</summary>
        public DateTime HolidayDate { get; set; }

        public string Reason { get; set; }

        public bool IsFullDay { get; set; }
    }

    public class CreateHolidayScheduleDto
    {
        /// <summary>Date in "yyyy-MM-dd" or any parseable date format.</summary>
        public DateTime HolidayDate { get; set; }

        public string Reason { get; set; }

        public bool IsFullDay { get; set; } = true;
    }

    public class UpdateHolidayScheduleDto
    {
        public DateTime HolidayDate { get; set; }

        public string Reason { get; set; }

        public bool IsFullDay { get; set; } = true;
    }
}
