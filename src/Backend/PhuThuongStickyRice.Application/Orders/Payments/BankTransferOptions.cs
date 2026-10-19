namespace PhuThuongStickyRice.Application.Orders.Payments
{
    /// <summary>
    /// Cấu hình tài khoản nhận tiền hiển thị cho khách tại bước thanh toán
    /// chuyển khoản (bind từ config section <c>BankTransfer</c>).
    /// Toàn bộ giá trị là thông tin hiển thị — điền tài khoản thật của quán,
    /// đến lúc đó các giá trị placeholder bên dưới sẽ được thay.
    /// </summary>
    public class BankTransferOptions
    {
        /// <summary>Tên ngân hàng nhận tiền (vd "Vietcombank").</summary>
        public string BankName { get; set; } = "CHƯA CẤU HÌNH — điền tên ngân hàng";

        /// <summary>VietQR bank code, e.g. "VCB" (Vietcombank), "TCB" (Techcombank), "MB" (MBBank).</summary>
        public string BankId { get; set; } = "VCB";

        /// <summary>Số tài khoản nhận tiền của quán.</summary>
        public string AccountNumber { get; set; } = "0000000000";

        /// <summary>Tên chủ tài khoản (hiển thị khi khách quét QR).</summary>
        public string AccountName { get; set; } = "XOI PHU THUONG";

        /// <summary>VietQR image template: "compact", "compact2" or "qr_only".</summary>
        public string QrTemplate { get; set; } = "compact2";
    }
}
