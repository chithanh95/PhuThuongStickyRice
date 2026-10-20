namespace PhuThuongStickyRice.Web.ConfigurationOptions
{
    /// <summary>
    /// Endpoint của API backend (WebAPI) mà web host gọi tới.
    /// Phân biệt 2 giá trị vì dev chạy 2 port khác nhau còn LAN/deploy chạy
    /// chung origin — logic chọn dùng endpoint nào nằm ở <c>Program.Main</c>.
    /// </summary>
    public class ResourceServer
    {
        /// <summary>Endpoint API dùng khi web chạy localhost (Web và API khác port).</summary>
        public string Endpoint { get; set; }

        /// <summary>Endpoint API công khai (dùng khi deploy/LAN, cùng origin với web).</summary>
        public string PublicEndpoint { get; set; }
    }
}
