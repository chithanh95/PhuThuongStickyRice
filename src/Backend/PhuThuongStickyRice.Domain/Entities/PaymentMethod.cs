namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Hình thức khách trả tiền cho đơn hàng.
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>COD — nhận hàng rồi trả tiền mặt.</summary>
        Cod = 0,

        /// <summary>Chuyển khoản qua VietQR — khách chuyển, admin xác nhận.</summary>
        BankTransfer = 1,

        /// <summary>Ví MoMo (đang chạy chế độ thử nghiệm/mô phỏng cho tới khi có merchant thật).</summary>
        MoMo = 2,

        /// <summary>Trừ số dư ví trả trước của khách (item 140).</summary>
        Wallet = 3,

        /// <summary>Nợ đơn — trả sau (item 50). Chỉ khách có ≥ 5 đơn hoàn thành mới được dùng.</summary>
        PayLater = 4
    }
}
