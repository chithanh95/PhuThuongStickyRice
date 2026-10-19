using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using Microsoft.AspNetCore.Components;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Lớp cơ sở cho mọi service gọi WebAPI phía Blazor. Tự động gắn bearer token
    /// (refresh khi hết hạn) vào request, và xử lý chung 2 lỗi hệ thống:
    /// 401 → đẩy về trang đăng nhập (kèm returnUrl), 404 → trang không tìm thấy.
    /// </summary>
    public class ApiClientBase
    {
        protected readonly HttpClient _httpClient;
        protected readonly ITokenStore _tokenStore;
        protected readonly NavigationManager _navigation;

        /// <summary>
        /// Khởi tạo client gọi API. <paramref name="navigation"/> chỉ có ở
        /// các service Blazor có NavigationManager (để xử lý 401/404);
        /// HttpClient thô truyền null.
        /// </summary>
        public ApiClientBase(HttpClient httpClient, ITokenStore tokenStore, NavigationManager navigation = null)
        {
            _httpClient = httpClient;
            _tokenStore = tokenStore;
            _navigation = navigation;
        }

        /// <summary>
        /// API base address đã cắt dấu "/" cuối (vd "https://api...").
        /// Dùng chung khi dựng URL tuyệt đối (xuất Excel, ảnh upload, mã QR)
        /// để không lặp lại biểu thức TrimEnd('/') ở từng service.
        /// </summary>
        protected string ApiBaseAddress =>
            _httpClient.BaseAddress?.ToString()?.TrimEnd('/');

        /// <summary>
        /// Dựng URL tuyệt đối cho ảnh do endpoint upload trả về. API phục vụ
        /// upload từ chính wwwroot của nó nên đường dẫn tương đối như
        /// /uploads/landing/xxx.jpg được resolve theo base address của API.
        /// URL đã tuyệt đối (http...) được giữ nguyên; rỗng/trắng trả null.
        /// Gom về đây từ LandingService/AdminCommerceService để không copy
        /// cùng 1 method ở 2 service (codequality-team 2026-10-20).
        /// </summary>
        /// <param name="url">Đường dẫn ảnh (tương đối hoặc tuyệt đối).</param>
        /// <returns>URL tuyệt đối, hoặc null khi đầu vào rỗng.</returns>
        public string ResolveImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var path = url.StartsWith('/') ? url : "/" + url;
            return string.IsNullOrEmpty(ApiBaseAddress) ? path : ApiBaseAddress + path;
        }

        /// <summary>
        /// 401 → session hết hạn: về trang đăng nhập.
        /// </summary>
        protected void HandleUnauthorized(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && _navigation != null)
            {
                var returnUrl = Uri.EscapeDataString(_navigation.ToBaseRelativePath(_navigation.Uri));
                _navigation.NavigateTo($"/dang-nhap?returnUrl={returnUrl}");
            }
        }

        /// <summary>
        /// 404 → về trang không tìm thấy.
        /// </summary>
        protected void HandleNotFound(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound && _navigation != null)
            {
                _navigation.NavigateTo("/not-found");
            }
        }

        /// <summary>
        /// Lấy access token hiện tại; tự refresh rồi đọc lại khi token đã hết hạn.
        /// Trả về null khi không có session (request đi ẩn danh).
        /// </summary>
        public async Task<string> GetAccessToken()
        {
            var token = await _tokenStore.GetToken();
            if (token == null)
            {
                return null;
            }

            if (token.IsTokenExpired)
            {
                await _tokenStore.RefreshToken();
                token = await _tokenStore.GetToken();
            }

            return token?.AccessToken;
        }

        /// <summary>
        /// Gắn bearer token vào HttpClient trước mỗi request. Bỏ qua khi
        /// token store đã tự gắn token ở mức handler (HttpClientFactory).
        /// </summary>
        protected async Task SetBearerToken()
        {
            if (_tokenStore.AttachTokenAutomatically)
            {
                return;
            }

            var accessToken = await GetAccessToken();
            if (accessToken != null)
            {
                _httpClient.UseBearerToken(accessToken);
            }
        }

        /// <summary>
        /// Gửi GET với bearer token, xử lý 401/404 chung, và giải mã response JSON.
        /// </summary>
        /// <typeparam name="T">Kiểu DTO của response.</typeparam>
        /// <param name="url">API endpoint tương đối (vd "api/orders/stats").</param>
        /// <returns>DTO đã giải mã từ response.</returns>
        protected async Task<T> GetAsync<T>(string url)
        {
            await SetBearerToken();

            // Dispose response/request: giải phóng connection về pool ngay,
            // đặc biệt với ResponseHeadersRead (content chưa được buffer).
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            HandleUnauthorized(response);
            HandleNotFound(response);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadAs<T>();
            return dto;
        }

        /// <summary>
        /// Gửi POST với JSON body, gắn bearer token, xử lý 401/404 chung,
        /// và giải mã response JSON. Bỏ qua body khi <paramref name="data"/> null.
        /// </summary>
        /// <typeparam name="T">Kiểu DTO của response.</typeparam>
        /// <param name="url">API endpoint tương đối (vd "api/orders").</param>
        /// <param name="data">Body gửi lên (serialize JSON); null thì không gửi body.</param>
        /// <returns>DTO đã giải mã từ response.</returns>
        protected async Task<T> PostAsync<T>(string url, object data = null)
        {
            await SetBearerToken();

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (data != null)
            {
                request.Content = data.AsJsonContent();
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            HandleUnauthorized(response);
            HandleNotFound(response);
            response.EnsureSuccessStatusCode();
            var createdObject = await response.Content.ReadAs<T>();
            return createdObject;
        }

        /// <summary>
        /// Gửi PUT với JSON body, gắn bearer token, xử lý 401/404 chung,
        /// và giải mã response JSON thành <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Kiểu DTO của response.</typeparam>
        /// <param name="url">API endpoint tương đối (vd "api/orders/{id}").</param>
        /// <param name="data">Body gửi lên (serialize JSON).</param>
        public async Task<T> PutAsync<T>(string url, object data)
        {
            await SetBearerToken();

            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = data.AsJsonContent();

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            HandleUnauthorized(response);
            HandleNotFound(response);
            response.EnsureSuccessStatusCode();
            var updatedObject = await response.Content.ReadAs<T>();
            return updatedObject;
        }

        /// <summary>
        /// Gửi DELETE với bearer token, xử lý 401/404 chung.
        /// </summary>
        /// <param name="url">API endpoint tương đối (vd "api/customer-tags/{id}").</param>
        protected async Task DeleteAsync(string url)
        {
            await SetBearerToken();

            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            HandleUnauthorized(response);
            HandleNotFound(response);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Trích message lỗi tiếng Việt từ body response lỗi dạng JSON
        /// (property "message"). Dùng chung cho các service upload multipart
        /// không qua <c>EnsureSuccessStatusCode</c> (LandingService,
        /// AdminCommerceService) để không viết lặp lại khối parse.
        /// </summary>
        /// <param name="body">Body text của response lỗi.</param>
        /// <returns>
        /// Giá trị property "message" khi parse được, ngược lại null — caller
        /// dùng message mặc định của mình.
        /// </returns>
        protected static string ParseServerErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                // JsonDocument giữ buffer bên dưới: dispose ngay sau khi đọc.
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                {
                    return msg.GetString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Cố ý bỏ qua: body lỗi không phải JSON thì caller giữ
                // message mặc định của nó.
            }

            return null;
        }
    }
}
