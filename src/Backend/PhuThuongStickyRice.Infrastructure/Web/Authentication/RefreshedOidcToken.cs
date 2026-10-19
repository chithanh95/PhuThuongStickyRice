using System;

namespace PhuThuongStickyRice.Infrastructure.Web.Authentication
{
    /// <summary>
    /// Cặp access token / refresh token vừa được làm mới từ nhà cung cấp OpenID Connect.
    /// </summary>
    public class RefreshedOidcToken
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public bool TokenExpired
        {
            get
            {
                return ExpiresAt.AddSeconds(-60).ToUniversalTime() <= DateTime.UtcNow;
            }
        }
    }
}
