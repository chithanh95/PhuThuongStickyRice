namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Trạng thái kinh doanh của sản phẩm trên shop. "Ngưng bán" (soft-delete)
    /// dùng <see cref="Inactive"/> hoặc <see cref="Discontinued"/> thay vì xóa
    /// cứng — giữ nguyên dữ liệu cho các đơn cũ đang trỏ tới sản phẩm.
    /// </summary>
    public enum ProductStatus
    {
        /// <summary>Đang bán: hiển thị trên shop.</summary>
        Active = 1,

        /// <summary>Tạm ẩn khỏi shop nhưng giữ dữ liệu (có thể mở bán lại).</summary>
        Inactive = 2,

        /// <summary>Ngừng bán hẳn: không hiển thị và không cho mở bán lại qua luồng thường.</summary>
        Discontinued = 3
    }
}
