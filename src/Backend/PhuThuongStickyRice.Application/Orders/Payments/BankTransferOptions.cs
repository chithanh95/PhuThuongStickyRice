namespace PhuThuongStickyRice.Application.Orders.Payments
{
    /// <summary>
    /// Bank-transfer settings (bound from the <c>BankTransfer</c> config section).
    /// All values are display info shown to the buyer at checkout — fill in the
    /// shop's real bank account. Until then the placeholders below are used.
    /// </summary>
    public class BankTransferOptions
    {
        public string BankName { get; set; } = "CHƯA CẤU HÌNH — điền tên ngân hàng";

        /// <summary>VietQR bank code, e.g. "VCB" (Vietcombank), "TCB" (Techcombank), "MB" (MBBank).</summary>
        public string BankId { get; set; } = "VCB";

        public string AccountNumber { get; set; } = "0000000000";

        public string AccountName { get; set; } = "XOI PHU THUONG";

        /// <summary>VietQR image template: "compact", "compact2" or "qr_only".</summary>
        public string QrTemplate { get; set; } = "compact2";
    }
}
