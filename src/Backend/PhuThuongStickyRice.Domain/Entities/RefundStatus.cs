namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Trạng thái hoàn tiền của đơn (item 130). Việc chuyển tiền thực tế
    /// là thủ công (tiền mặt / chuyển khoản / cộng vào ví); trường này
    /// chỉ ghi lại trạng thái để theo dõi.
    /// </summary>
    public enum RefundStatus
    {
        /// <summary>Chưa yêu cầu hoàn tiền.</summary>
        None = 0,

        /// <summary>Đã yêu cầu hoàn, chưa thực hiện chuyển tiền.</summary>
        Requested = 1,

        /// <summary>Đã hoàn tiền xong (ghi nhận).</summary>
        Refunded = 2
    }
}
