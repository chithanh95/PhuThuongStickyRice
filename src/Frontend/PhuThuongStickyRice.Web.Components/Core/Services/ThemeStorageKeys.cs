namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Key localStorage cho chế độ sáng/tối của từng web. Phải khớp với key
    /// mà file JS theme (ptSetTheme / ptIsDark / ptApplyTheme / ptGetTheme)
    /// đang đọc/ghi — đổi key ở đây mà không đổi JS sẽ mất theme đã lưu.
    /// </summary>
    public static class ThemeStorageKeys
    {
        /// <summary>Key theme của web shop (trang chủ, đăng nhập, quên mật khẩu...).</summary>
        public const string Shop = "pt-theme-shop";

        /// <summary>Key theme của web admin.</summary>
        public const string Admin = "pt-theme-admin";
    }
}
