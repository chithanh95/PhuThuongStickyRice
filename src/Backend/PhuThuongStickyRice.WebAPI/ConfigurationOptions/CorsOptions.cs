namespace PhuThuongStickyRice.WebAPI.ConfigurationOptions
{
    /// <summary>
    /// Tùy chọn CORS cho WebAPI (bind từ section "CORS" trong appsettings.json).
    /// Đặt tên theo chuẩn .NET: acronym trong tên type dùng PascalCase (như CorsPolicy).
    /// </summary>
    public class CorsOptions
    {
        public bool AllowAnyOrigin { get; set; }

        public string[] AllowedOrigins { get; set; }
    }
}
