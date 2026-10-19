using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Web.Authentication
{
    /// <summary>
    /// Làm mới access token qua OpenID Connect (dùng refresh token) cho WebAPI.
    /// </summary>
    public class OpenIdConnectTokenRefresher
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenIdConnectOptions _options;

        public OpenIdConnectTokenRefresher(IHttpClientFactory httpClientFactory, OpenIdConnectOptions options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
        }

        /// <summary>
        /// Đổi refresh token lấy access token mới qua OIDC discovery + refresh flow.
        /// Trả về null khi refresh token đã hết hạn/không hợp lệ (HTTP 400).
        /// </summary>
        /// <param name="refreshToken">Refresh token hiện tại của user.</param>
        /// <returns>Token mới, hoặc null khi không refresh được.</returns>
        public async Task<RefreshedOidcToken> RefreshToken(string refreshToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            // IdentityModel 6.x: DiscoveryDocumentResponse/TokenResponse KHÔNG
            // implement IDisposable (đã đọc hết body vào string Raw bên trong),
            // nên không cần using — compiler báo CS1674 nếu cố dùng using.
            var metaDataResponse = await httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _options.Authority,
                Policy = { RequireHttps = _options.RequireHttpsMetadata },
            });

            var response = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = metaDataResponse.TokenEndpoint,
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret,
                RefreshToken = refreshToken,
            });

            if (response.IsError)
            {
                if (response.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return null;
                }

                throw new Exception(response.Raw);
            }

            return new RefreshedOidcToken
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                ExpiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn),
            };
        }
    }
}
