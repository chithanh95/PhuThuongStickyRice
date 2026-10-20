namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Nhãn và tham số hiển thị dùng chung cho các màn báo cáo
    /// (OrderReports, GrossProfitReport, OrderHourlyReport, WeeklyReport).
    /// Gom các bản copy-paste trước đây (mỗi service tự khai báo hằng riêng)
    /// về 1 chỗ để đổi 1 lần là áp dụng khắp nơi, tránh các bản sao lệch
    /// nhau dần (ví dụ nhãn "(không tên)" chỗ có chỗ không).
    /// </summary>
    internal static class ReportLabels
    {
        /// <summary>
        /// Nhãn hiển thị cho món không có tên (dòng legacy/combo thiếu tên
        /// món trong báo cáo). Thống nhất 1 nhãn cho mọi báo cáo để khách
        /// và nhân viên không thấy "(không tên)" chỗ này, "N/A" chỗ khác.
        /// </summary>
        public const string UnnamedProductLabel = "(không tên)";

        /// <summary>
        /// Số món bán chạy nhất hiển thị trong phần "top sản phẩm" của các
        /// báo cáo (doanh thu, báo cáo tuần). Gom 2 bản copy-paste trước đây
        /// ở OrderReportsAppService và WeeklyReportSender về 1 chỗ.
        /// </summary>
        public const int TopProductsCount = 5;
    }
}
