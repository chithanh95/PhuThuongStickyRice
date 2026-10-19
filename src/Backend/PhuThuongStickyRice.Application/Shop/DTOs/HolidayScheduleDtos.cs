using System;

namespace PhuThuongStickyRice.Application.Shop.DTOs
{
    /// <summary>
    /// Lịch nghỉ lễ của quán: ngày nghỉ và lý do. Ngày nghỉ cả ngày (IsFullDay)
    /// khiến quán tự động ngừng nhận đơn ở checkout (item 117).
    /// </summary>
    public class HolidayScheduleDto
    {
        public Guid Id { get; set; }

        /// <summary>The holiday date (date part only).</summary>
        public DateTime HolidayDate { get; set; }

        public string Reason { get; set; }

        public bool IsFullDay { get; set; }
    }

    /// <summary>
    /// Dữ liệu admin thêm ngày nghỉ lễ. Mặc định IsFullDay = true (nghỉ cả ngày,
    /// ngừng nhận đơn); để false khi chỉ nghỉ một buổi.
    /// </summary>
    public class CreateHolidayScheduleDto
    {
        /// <summary>Date in "yyyy-MM-dd" or any parseable date format.</summary>
        public DateTime HolidayDate { get; set; }

        public string Reason { get; set; }

        public bool IsFullDay { get; set; } = true;
    }

    /// <summary>
    /// Dữ liệu admin sửa ngày nghỉ lễ (cùng các trường với lúc tạo mới).
    /// </summary>
    public class UpdateHolidayScheduleDto
    {
        public DateTime HolidayDate { get; set; }

        public string Reason { get; set; }

        public bool IsFullDay { get; set; } = true;
    }
}
