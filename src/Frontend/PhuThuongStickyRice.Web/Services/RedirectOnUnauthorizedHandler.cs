using PhuThuongStickyRice.Web.Components.Core.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Services
{
    /// <summary>
    /// Handler HTTP gắn vào MỌI API client (đăng ký trong Program.cs qua
    /// AddApiHttpClient): khi API trả 401 — token hết hạn hoặc không hợp
    /// lệ — thì xóa token cũ và điều hướng về trang đăng nhập kèm
    /// returnUrl để đăng nhập xong quay lại đúng trang đang làm việc.
    /// Trước đây việc này nằm ở ApiClientBase.HandleUnauthorized nhưng
    /// không service nào truyền NavigationManager vào (26/26 thiếu) nên
    /// không bao giờ chạy — người dùng hết token chỉ thấy trang lỗi.
    /// Hai trường hợp bỏ qua: (1) chính endpoint đăng nhập — 401 ở đó
    /// nghĩa là sai mật khẩu, trang login tự hiện lỗi; (2) đang đứng sẵn
    /// ở trang đăng nhập — tránh vòng lặp điều hướng.
    /// </summary>
    public class RedirectOnUnauthorizedHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigation;
        private readonly ITokenStore _tokenStore;

        /// <summary>Khởi tạo handler với NavigationManager (điều hướng) và kho token (xóa token hết hạn).</summary>
        public RedirectOnUnauthorizedHandler(NavigationManager navigation, ITokenStore tokenStore)
        {
            _navigation = navigation;
            _tokenStore = tokenStore;
        }

        /// <summary>
        /// Gửi request như bình thường; nếu response là 401 (và không thuộc
        /// trường hợp bỏ qua) thì dọn token và chuyển về trang đăng nhập
        /// trước khi trả response về cho caller xử lý tiếp.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized || IsAuthEndpoint(request.RequestUri) || IsOnLoginPage())
            {
                return response;
            }

            // Xóa token cũ để trạng thái đăng nhập sạch hẳn; lỗi khi xóa
            // (JS interop) cũng không được chặn việc điều hướng về login.
            try
            {
                await _tokenStore.ClearTokenAsync();
            }
            catch
            {
                // Bỏ qua: điều hướng về trang đăng nhập là việc chính.
            }

            // Trang đích theo khu vực (LoginRoutes): nhân viên về /login
            // (email), khách về /dang-nhap (SĐT) — trước đây cứng /dang-nhap
            // nên admin hết token giữa ca là không đăng nhập lại được.
            _navigation.NavigateTo(LoginRoutes.GetLoginUrl(_navigation.ToBaseRelativePath(_navigation.Uri)));
            return response;
        }

        /// <summary>
        /// Endpoint xác thực tự xử lý lỗi 401 của nó (sai mật khẩu, refresh
        /// thất bại) — handler không can thiệp.
        /// </summary>
        private static bool IsAuthEndpoint(Uri? uri)
        {
            var path = uri?.AbsolutePath ?? string.Empty;
            return path.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Đang ở một trang đăng nhập (/dang-nhap hoặc /login) thì không điều hướng nữa (tránh lặp).</summary>
        private bool IsOnLoginPage()
        {
            return LoginRoutes.IsLoginPath(_navigation.ToBaseRelativePath(_navigation.Uri));
        }
    }
}
