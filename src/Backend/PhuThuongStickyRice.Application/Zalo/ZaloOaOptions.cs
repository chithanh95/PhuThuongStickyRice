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
        /// <summary>Access token của Zalo OA, dùng để gọi API gửi tin Zalo. "xxx" = chưa cấu hình.</summary>
        public string AccessToken { get; set; } = "xxx";

        /// <summary>Id của Zalo Official Account (OA) của quán. "xxx" = chưa cấu hình.</summary>
        public string OaId { get; set; } = "xxx";

        /// <summary>
        /// True khi cả token và OA id đều đã được điền thật (khác "xxx") —
        /// service Zalo chỉ gửi tin thật khi flag này true, còn lại chỉ log.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccessToken)
            && AccessToken != "xxx"
            && !string.IsNullOrWhiteSpace(OaId)
            && OaId != "xxx";
    }
}
