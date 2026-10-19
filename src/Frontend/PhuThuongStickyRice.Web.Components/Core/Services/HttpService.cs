using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using Microsoft.AspNetCore.Components;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    public class HttpService
    {
        protected readonly HttpClient _httpClient;
        protected readonly ITokenManager _tokenManager;
        protected readonly NavigationManager _navigation;

        public HttpService(HttpClient httpClient, ITokenManager tokenManager, NavigationManager navigation = null)
        {
            _httpClient = httpClient;
            _tokenManager = tokenManager;
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

        public async Task<string> GetAccessToken()
        {
            var token = await _tokenManager.GetToken();
            if (token == null)
            {
                return null;
            }

            if (token.TokenExpired)
            {
                await _tokenManager.RefreshToken();
                token = await _tokenManager.GetToken();
            }

            return token?.AccessToken;
        }

        protected async Task SetBearerToken()
        {
            if (_tokenManager.AttachTokenAutomatically)
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
