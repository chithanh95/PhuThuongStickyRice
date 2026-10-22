using System;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Quyết định trang đăng nhập theo khu vực của route hiện tại:
    /// khu vực NHÂN VIÊN (các trang dùng AdminLayout: /admin, /dashboard,
    /// /orders, /products, /customers) đăng nhập ở /login bằng email +
    /// mật khẩu (+ 2FA); khu vực KHÁCH đăng nhập ở /dang-nhap bằng số
    /// điện thoại. Trước đây cả 3 điểm điều hướng (RedirectToLogin,
    /// RedirectOnUnauthorizedHandler, ApiClientBase) đều cứng về
    /// /dang-nhap nên tài khoản admin không đăng nhập lại được khi mở
    /// thẳng link admin hoặc khi token hết hạn giữa ca (user báo
    /// 2026-10-22). Mọi điểm cần điều hướng về login phải qua helper này.
    /// </summary>
    public static class LoginRoutes
    {
        /// <summary>
        /// Tiền tố route (không có "/" đầu) thuộc khu vực nhân viên —
        /// khớp theo cả segment: bằng tiền tố hoặc tiền tố + "/".
        /// </summary>
        private static readonly string[] StaffPrefixes =
        {
            "admin", "dashboard", "orders", "products", "customers"
        };

        /// <summary>
        /// Có phải route nhân viên hay không. Nhận path tương đối so với
        /// base (có hoặc không có "/" đầu, có thể kèm query string).
        /// </summary>
        /// <param name="baseRelativePath">Đường dẫn tương đối hiện tại.</param>
        /// <returns>True nếu thuộc khu vực nhân viên.</returns>
        public static bool IsStaffPath(string baseRelativePath)
        {
            var path = (baseRelativePath ?? string.Empty).TrimStart('/').Split('?')[0].TrimEnd('/');
            foreach (var prefix in StaffPrefixes)
            {
                if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Có đang đứng ở một trang đăng nhập hay không (để tránh vòng lặp
        /// điều hướng khi API trả 401 ngay tại trang login).
        /// </summary>
        /// <param name="baseRelativePath">Đường dẫn tương đối hiện tại.</param>
        /// <returns>True nếu đang ở /dang-nhap hoặc /login.</returns>
        public static bool IsLoginPath(string baseRelativePath)
        {
            var path = (baseRelativePath ?? string.Empty).TrimStart('/');
            return path.StartsWith("dang-nhap", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("login", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Dựng URL trang đăng nhập đúng khu vực, kèm returnUrl (đường dẫn
        /// tuyệt đối bắt đầu bằng "/", đã escape) để đăng nhập xong quay
        /// lại đúng trang đang làm việc.
        /// </summary>
        /// <param name="baseRelativePath">Đường dẫn tương đối hiện tại (từ NavigationManager.ToBaseRelativePath).</param>
        /// <returns>URL trang đăng nhập kèm returnUrl.</returns>
        public static string GetLoginUrl(string baseRelativePath)
        {
            var relative = (baseRelativePath ?? string.Empty).TrimStart('/');
            var returnUrl = Uri.EscapeDataString("/" + relative);
            var loginPage = IsStaffPath(relative) ? "/login" : "/dang-nhap";
            return $"{loginPage}?returnUrl={returnUrl}";
        }
    }
}
