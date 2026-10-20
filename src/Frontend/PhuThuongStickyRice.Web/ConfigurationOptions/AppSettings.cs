namespace PhuThuongStickyRice.Web.ConfigurationOptions
{
    /// <summary>
    /// Cấu hình web host Blazor WASM, bind từ appsettings.json:
    /// thông tin API backend (<see cref="ResourceServer"/>).
    /// </summary>
    public class AppSettings
    {
        /// <summary>Thông tin endpoint của API backend (WebAPI).</summary>
        public ResourceServer ResourceServer { get; set; }
    }
}
