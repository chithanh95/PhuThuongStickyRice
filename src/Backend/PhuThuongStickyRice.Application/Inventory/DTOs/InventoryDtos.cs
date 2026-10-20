using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Inventory.DTOs
{
    /// <summary>
    /// Một dòng trong phiếu kiểm kê: mã món và số lượng còn lại đếm thực tế.
    /// </summary>
    public class InventoryLineInput
    {
        /// <summary>Id món được kiểm kê.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Số lượng còn lại đếm thực tế của món.</summary>
        public int RemainingQty { get; set; }

        /// <summary>Ghi chú của nhân viên về dòng kiểm kê (vd: món hư hỏng).</summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// Dữ liệu lưu phiếu kiểm kê tồn kho của một ngày kinh doanh.
    /// </summary>
    public class SaveDailyInventoryInput
    {
        /// <summary>Business day counted (date part is used).</summary>
        public DateTime Date { get; set; }

        /// <summary>Các dòng kiểm kê của ngày.</summary>
        public List<InventoryLineInput> Lines { get; set; } = new List<InventoryLineInput>();
    }

    /// <summary>
    /// Một dòng trong báo cáo kiểm kê theo ngày: số lượng còn lại, chênh
    /// lệch so với lần đếm trước và người thực hiện kiểm kê.
    /// </summary>
    public class DailyInventoryLineDto
    {
        /// <summary>Id món được kiểm kê.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Tên món (để admin đọc, không cần tra catalog).</summary>
        public string ProductName { get; set; }

        /// <summary>Số lượng còn lại tại lần đếm này.</summary>
        public int RemainingQty { get; set; }

        /// <summary>Ghi chú của nhân viên khi đếm món này.</summary>
        public string Note { get; set; }

        /// <summary>
        /// Remaining qty at the most recent earlier count of the same
        /// product, if any — used to show the day-over-day difference.
        /// </summary>
        public int? PreviousRemainingQty { get; set; }

        /// <summary>Chênh lệch so với lần đếm trước (âm = thiếu hụt); null khi chưa có lần đếm trước.</summary>
        public int? DiffVsPrevious => PreviousRemainingQty.HasValue
            ? RemainingQty - PreviousRemainingQty.Value
            : (int?)null;

        /// <summary>Tên đăng nhập của nhân viên thực hiện kiểm kê.</summary>
        public string CreatedBy { get; set; }

        /// <summary>Thời điểm ghi nhận kiểm kê (UTC).</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Phiếu kiểm kê tồn kho của một ngày: ngày kinh doanh và các dòng kiểm kê.
    /// </summary>
    public class DailyInventoryDayDto
    {
        /// <summary>Ngày kinh doanh được kiểm kê (phần ngày).</summary>
        public DateTime Date { get; set; }

        /// <summary>Các dòng kiểm kê trong ngày.</summary>
        public List<DailyInventoryLineDto> Lines { get; set; } = new List<DailyInventoryLineDto>();
    }
}
