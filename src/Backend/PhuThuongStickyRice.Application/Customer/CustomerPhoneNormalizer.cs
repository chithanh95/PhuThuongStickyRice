namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Chuẩn hóa SĐT dùng chung cho các service khách hàng: trim khoảng trắng,
    /// trả null khi rỗng. Dùng ở lớp service khi so khớp SĐT theo đúng chuỗi
    /// đã lưu trong DB (không đổi đầu số như
    /// <c>VietnamTime.NormalizePhone</c> — chỉ dùng helper này khi cần giữ
    /// nguyên định dạng SĐT gốc).
    /// </summary>
    internal static class CustomerPhoneNormalizer
    {
        /// <summary>
        /// Trim SĐT; trả null khi SĐT rỗng để caller xử lý thống nhất.
        /// </summary>
        /// <param name="phone">SĐT thô từ input.</param>
        /// <returns>SĐT đã trim, hoặc null khi rỗng.</returns>
        internal static string NormalizePhone(string phone) =>
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
