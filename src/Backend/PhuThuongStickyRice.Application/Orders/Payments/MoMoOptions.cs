namespace PhuThuongStickyRice.Application.Orders.Payments
{
    /// <summary>
    /// MoMo settings (bound from the <c>MoMo</c> config section).
    /// Leave PartnerCode/AccessKey/SecretKey EMPTY until the shop has a real
    /// MoMo business account — TestMode must stay <c>true</c> until then.
    /// </summary>
    public class MoMoOptions
    {
        public string PartnerCode { get; set; } = string.Empty;

        public string AccessKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// MoMo create-payment endpoint. Test: https://test-payment.momo.vn/v2/gateway/api/create
        /// Production: https://payment.momo.vn/v2/gateway/api/create
        /// </summary>
        public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

        /// <summary>
        /// When true, the real MoMo endpoint is NEVER called: the buyer is sent
        /// to the in-app MoMo simulator page instead.
        /// </summary>
        public bool TestMode { get; set; } = true;

        /// <summary>Base URL of the Blazor storefront (used to build the simulator / return URL).</summary>
        public string StorefrontBaseUrl { get; set; } = "https://localhost:44348";

        /// <summary>Base URL of this WebAPI (used to build the real MoMo ipnUrl).</summary>
        public string ApiBaseUrl { get; set; } = "https://localhost:44312";
    }
}
