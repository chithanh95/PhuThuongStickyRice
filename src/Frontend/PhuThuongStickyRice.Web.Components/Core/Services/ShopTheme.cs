using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Đọc/ghi chế độ sáng/tối của web shop qua JS interop (fail-soft: lỗi JS
    /// thì giữ mặc định, không ảnh hưởng chức năng trang). Toggle NHỊ PHÂN
    /// Sáng↔Tối — 1 lần bấm = đổi giao diện ngay (không xoay vòng 3 chế độ
    /// như ptToggleTheme vì ban đêm bấm từ "tối" sang "tự động" chỉ đổi icon
    /// mà màn vẫn đen).
    /// </summary>
    public static class ShopTheme
    {
        /// <summary>
        /// Đọc trạng thái dark mode shop đã lưu trong localStorage.
        /// </summary>
        /// <returns>true khi đang ở dark mode; false khi lỗi JS/chưa lưu.</returns>
        public static async Task<bool> ReadIsDarkAsync(IJSRuntime js)
        {
            try
            {
                return await js.InvokeAsync<bool>("ptIsDark", ThemeStorageKeys.Shop);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lưu và áp theme shop theo cờ <paramref name="isDark"/>.
        /// </summary>
        public static async Task ApplyAsync(IJSRuntime js, bool isDark)
        {
            try
            {
                await js.InvokeVoidAsync("ptSetTheme", ThemeStorageKeys.Shop, isDark ? "dark" : "light");
            }
            catch
            {
                // Bỏ qua: theme chỉ ảnh hưởng giao diện, không chặn chức năng.
            }
        }
    }
}
