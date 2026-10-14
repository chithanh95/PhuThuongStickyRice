namespace PhuThuongStickyRice.Application.Zalo
{
    /// <summary>
    /// Zalo OA (Official Account) settings, bound from the <c>ZaloOa</c>
    /// config section. Values are placeholders ("xxx") until the shop
    /// creates a Zalo OA and issues an access token — see README for the
    /// setup steps. With placeholders the service only logs.
    /// </summary>
    public class ZaloOaOptions
    {
        public string AccessToken { get; set; } = "xxx";

        public string OaId { get; set; } = "xxx";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccessToken)
            && AccessToken != "xxx"
            && !string.IsNullOrWhiteSpace(OaId)
            && OaId != "xxx";
    }
}
