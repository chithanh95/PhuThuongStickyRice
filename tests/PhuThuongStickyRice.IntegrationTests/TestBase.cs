using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using PhuThuongStickyRice.IntegrationTests.Configuration;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.IntegrationTests
{
    /// <summary>
    /// Lớp cơ sở cho integration test: đọc cấu hình WebAPI từ appsettings và
    /// cung cấp <see cref="HttpClient"/> dùng chung. CA1001: class sở hữu
    /// field disposable phải implement <see cref="IDisposable"/> — xUnit tự
    /// dispose mỗi instance test class sau khi chạy xong.
    /// </summary>
    public class TestBase : IDisposable
    {
        public AppSettings AppSettings { get; set; } = new AppSettings();

        // CA1051: field instance visible (protected) → property. Khởi tạo
        // 1 lần duy nhất; Dispose ở cuối mỗi test để không rò rỉ socket.
        protected HttpClient HttpClient { get; } = new HttpClient();

        private bool _disposed;

        public TestBase()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            configuration.Bind(AppSettings);

            HttpClient.BaseAddress = new Uri(AppSettings.WebAPI.Endpoint);
        }

        /// <summary>Giải phóng <see cref="HttpClient"/> dùng chung của test.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Pattern dispose chuẩn: cho phép lớp con override.</summary>
        /// <param name="disposing">True khi dispose chủ động (không phải finalizer).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                HttpClient.Dispose();
            }

            _disposed = true;
        }

        protected async Task GetTokenAsync()
        {
            var metaDataResponse = await HttpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = AppSettings.OpenIdConnect.Authority,
                Policy = { RequireHttps = AppSettings.OpenIdConnect.RequireHttpsMetadata },
            });

            var tokenResponse = await HttpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = metaDataResponse.TokenEndpoint,
                ClientId = AppSettings.OpenIdConnect.ClientId,
                ClientSecret = AppSettings.OpenIdConnect.ClientSecret,
                UserName = AppSettings.Login.UserName,
                Password = AppSettings.Login.Password,
                Scope = AppSettings.Login.Scope,
            });

            var token = tokenResponse.AccessToken;
            HttpClient.UseBearerToken(token);
        }

        protected async Task<T> GetAsync<T>(string url)
        {
            var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAs<T>();
            return data;
        }

        protected async Task<T> PostAsync<T>(string url, object data = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (data != null)
            {
                request.Content = data.AsJsonContent();
            }

            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var createdObject = await response.Content.ReadAs<T>();
            return createdObject;
        }

        public async Task<T> PutAsync<T>(string url, object data)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = data.AsJsonContent();

            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var updatedObject = await response.Content.ReadAs<T>();
            return updatedObject;
        }

        protected async Task DeleteAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        }
    }
}
