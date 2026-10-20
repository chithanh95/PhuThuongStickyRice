namespace PhuThuongStickyRice.WebAPI.RateLimiterPolicies
{
    /// <summary>
    /// Tên các rate-limiter policy. Policy mặc định được gắn cho hầu hết
    /// controller qua attribute
    /// <c>[EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]</c>.
    /// </summary>
    public static class RateLimiterPolicyNames
    {
        /// <summary>Policy rate-limit mặc định áp cho các API endpoint.</summary>
        public const string DefaultPolicy = "DefaultPolicy";
    }
}
