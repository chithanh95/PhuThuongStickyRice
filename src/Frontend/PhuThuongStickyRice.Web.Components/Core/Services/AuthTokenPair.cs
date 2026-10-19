using System;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Cặp access/refresh token đang lưu ở client Blazor WASM.
    /// </summary>
    public class AuthTokenPair
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public bool IsTokenExpired
        {
            get
            {
                return ExpiresAt.ToUniversalTime() <= DateTime.UtcNow.AddSeconds(60);
            }
        }
    }
}
