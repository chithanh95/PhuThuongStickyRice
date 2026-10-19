using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Item 233: danh sách lý do hủy đơn chuẩn — khách chọn khi tự hủy,
    /// nhân viên chọn khi hủy tay. Dùng chung backend + frontend để báo cáo
    /// tỉ lệ hủy theo lý do được nhất quán.
    /// </summary>
    public static class OrderCancelReasons
    {
        public const string ChangedMind = "Đổi ý";
        public const string WrongOrder = "Đặt nhầm món";
        public const string TooLongWait = "Chờ quá lâu";
        public const string OutOfStock = "Món hết hàng";
        public const string Other = "Lý do khác";

        /// <summary>Lý do hệ thống tự ghi khi auto-cancel đơn quá hạn thanh toán.</summary>
        public const string SystemExpiredPayment = "Quá hạn thanh toán";

        /// <summary>Danh sách hiển thị cho khách/nhân viên chọn.</summary>
        public static readonly IReadOnlyList<string> All = new List<string>
        {
            ChangedMind,
            WrongOrder,
            TooLongWait,
            OutOfStock,
            Other,
        };
    }
}
