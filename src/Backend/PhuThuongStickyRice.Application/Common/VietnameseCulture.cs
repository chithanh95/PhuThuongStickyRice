using System.Globalization;

namespace PhuThuongStickyRice.Application.Common;

/// <summary>
/// Culture tiếng Việt dùng chung khi định dạng tiền/ngày/tên thứ hiển thị
/// cho người dùng (vd "50.000₫", "Thứ Hai").
/// [codequality-smell 2026-10-20] Gom chuỗi "vi-VN" bị hardcode ở 4 service
/// (OrderForecastAppService, CorporateClientAppService,
/// IngredientForecastAppService, WeeklyReportSender) về một chỗ — đổi locale
/// hiển thị chỉ cần sửa 1 file.
/// </summary>
public static class VietnameseCulture
{
    /// <summary>
    /// Culture "vi-VN" (instance cache sẵn, read-only): định dạng tiền ₫,
    /// ngày dd/MM/yyyy, tên thứ tiếng Việt.
    /// </summary>
    public static readonly CultureInfo Value = CultureInfo.GetCultureInfo("vi-VN");
}
