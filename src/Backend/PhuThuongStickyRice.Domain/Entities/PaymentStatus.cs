namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Trạng thái thanh toán của đơn, độc lập với trạng thái giao hàng <see cref="OrderStatus"/>.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>Chưa thanh toán (COD chờ giao, hoặc chuyển khoản/MoMo chưa bắt đầu).</summary>
        Unpaid = 0,

        /// <summary>Khách báo đã chuyển khoản, đang chờ admin xác nhận.</summary>
        PendingConfirmation = 1,

        /// <summary>Đã thanh toán (admin xác nhận chuyển khoản, hoặc MoMo báo thành công).</summary>
        Paid = 2,

        /// <summary>Thanh toán thất bại (MoMo báo lỗi).</summary>
        Failed = 3
    }
}
