namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// SMS settings, bound from the <c>Sms</c> config section. Values are
    /// placeholders ("xxx") until the shop contracts a real SMS provider
    /// (e.g. Viettel, VNPT, SpeedSMS) — with placeholders the
    /// <see cref="LoggingSmsSender"/> only logs the message.
    /// </summary>
    public class SmsOptions
    {
        /// <summary>Provider name: "Logging" (default), or a real provider key.</summary>
        public string Provider { get; set; } = "Logging";

        public string ApiKey { get; set; } = "xxx";

        public string SenderName { get; set; } = "XoiPhuThuong";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ApiKey)
            && ApiKey != "xxx";
    }
}
