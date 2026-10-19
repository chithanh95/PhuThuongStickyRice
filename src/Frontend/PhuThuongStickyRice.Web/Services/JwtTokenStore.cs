using PhuThuongStickyRice.Web.Components.Core.Services;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Services
{
    /// <summary>
    /// JWT token storage (browser localStorage) for the in-repo admin login.
    /// Tokens are issued by WebAPI itself — no OIDC / external IdentityServer.
    /// </summary>
    public class JwtTokenStore : ITokenManager
    {
        private const string TokenKey = "phuthuong_access_token";
        private const string ExpiresKey = "phuthuong_access_token_expires";

        private readonly IJSRuntime _jsRuntime;

        public JwtTokenStore(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>Tokens are attached manually by HttpService, not by a message handler.</summary>
        public bool AttachTokenAutomatically => false;

        public async Task<AuthToken> GetToken()
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var expiresText = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ExpiresKey);
            DateTimeOffset.TryParse(expiresText, out var expiresAt);

            return new AuthToken
            {
                AccessToken = token,
                ExpiresAt = expiresAt
            };
        }

        /// <summary>No refresh tokens — the admin logs in again when the token expires.</summary>
        public Task RefreshToken() => Task.CompletedTask;

        public async Task StoreTokenAsync(string token, DateTimeOffset expiresAt)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ExpiresKey, expiresAt.ToString("O"));
        }

        public async Task ClearTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ExpiresKey);
        }
    }
}
