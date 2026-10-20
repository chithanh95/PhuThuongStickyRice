using ClosedXML.Excel;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Helper dùng chung cho các file Excel xuất ra (.xlsx): style hàng tiêu
    /// đề và độ rộng cột cố định. Gộp từ các bản copy-paste trước đây:
    /// <c>CustomerTagAppService</c> (style header tím + độ rộng cột cố định)
    /// và <c>OrderReportsAppService</c> (style header + độ rộng cột cố định
    /// riêng). Style của mọi sheet giờ nằm 1 chỗ nên đổi màu header là đổi
    /// đồng loạt cả hệ thống.
    /// </summary>
    internal static class ExcelWorksheetStyling
    {
        /// <summary>
        /// Màu nền tím thương hiệu (#7a6fbe) của hàng tiêu đề file Excel.
        /// </summary>
        public const string BrandHeaderBackground = "#7a6fbe";

        /// <summary>
        /// Style hàng tiêu đề file Excel: chữ đậm, căn giữa, nền màu thương
        /// hiệu hoặc màu truyền vào (chữ trắng khi nền tối, chữ đen khi nền
        /// sáng như header vàng #f5c518 của sheet xuất đơn hàng).
        /// </summary>
        /// <param name="headerRow">Hàng tiêu đề (thường là dòng 1).</param>
        /// <param name="backgroundHex">Màu nền dạng hex (vd #7a6fbe).</param>
        /// <param name="whiteFont">True = chữ trắng (nền tối), false = chữ đen (nền sáng).</param>
        /// <param name="center">True = căn giữa nội dung ô.</param>
        internal static void StyleHeader(
            IXLRow headerRow,
            string backgroundHex = BrandHeaderBackground,
            bool whiteFont = true,
            bool center = true)
        {
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml(backgroundHex);
            headerRow.Style.Font.FontColor = whiteFont ? XLColor.White : XLColor.Black;
            if (center)
            {
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        /// <summary>
        /// Đặt độ rộng cột cố định cho sheet export (thay
        /// <c>AdjustToContents()</c>): ClosedXML đo font từng cell khi
        /// auto-fit — O(dòng × cột), export 5000 dòng tốn vài giây chỉ để căn
        /// cột. Độ rộng cố định render ngay lập tức, không đổi dữ liệu trong
        /// cell (chữ dài vẫn đầy đủ, chỉ không tự giãn cột).
        /// </summary>
        /// <param name="sheet">Sheet cần đặt độ rộng.</param>
        /// <param name="widths">Độ rộng từng cột, theo thứ tự cột 1..N.</param>
        internal static void SetFixedColumnWidths(IXLWorksheet sheet, params double[] widths)
        {
            for (var i = 0; i < widths.Length; i++)
            {
                sheet.Column(i + 1).Width = widths[i];
            }
        }
    }
}
