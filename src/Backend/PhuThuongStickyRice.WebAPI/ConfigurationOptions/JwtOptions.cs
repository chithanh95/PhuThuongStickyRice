namespace PhuThuongStickyRice.WebAPI.ConfigurationOptions
{
    /// <summary>
    /// Self-issued JWT settings (bound from the <c>Jwt</c> config section).
    /// The API issues and validates its own tokens — no external IdentityServer.
    /// </summary>
    public class JwtOptions
    {
        public string Key { get; set; }

        public string Issuer { get; set; } = "PhuThuongStickyRice.WebAPI";

        public string Audience { get; set; } = "PhuThuongStickyRice.WebAPI";

        /// <summary>Token lifetime in minutes.</summary>
        public int ExpiryMinutes { get; set; } = 480;
    }
}
