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
    public class HttpService
    {
        protected readonly HttpClient _httpClient;
        protected readonly ITokenStore _tokenStore;
        protected readonly NavigationManager _navigation;

        public HttpService(HttpClient httpClient, ITokenStore tokenStore, NavigationManager navigation = null)
        {
            _httpClient = httpClient;
            _tokenStore = tokenStore;
            _navigation = navigation;
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

            if (token.TokenExpired)
            {
                await _tokenStore.RefreshToken();
                token = await _tokenStore.GetToken();
            }

            return token?.AccessToken;
        }

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

        protected async Task<T> GetAsync<T>(string url)
        {
            await SetBearerToken();

            // Dispose response/request: giải phóng connection về pool ngay,
            // đặc biệt với ResponseHeadersRead (content chưa được buffer).
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            HandleUnauthorized(response);
            HandleNotFound(response);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAs<T>();
            return data;
        }

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
    }
}
