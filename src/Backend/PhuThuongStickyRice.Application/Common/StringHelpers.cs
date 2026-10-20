namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Helper chuỗi dùng chung cho các service: chuẩn hóa chuỗi nullable
    /// (trim, rỗng/trắng thì trả null). Gộp pattern lặp y hệt ở nhiều
    /// service (trước đây mỗi service tự viết 1 bản private/static) — các
    /// field optional như ghi chú, địa chỉ, SĐT đều dùng pattern này để DB
    /// lưu null thay vì chuỗi trắng.
    /// </summary>
    internal static class StringHelpers
    {
        /// <summary>
        /// Trim chuỗi; rỗng/trắng thì trả null — các field optional (ghi chú,
        /// địa chỉ, SĐT...) dùng pattern này để DB lưu null thay vì chuỗi trắng.
        /// </summary>
        /// <param name="value">Chuỗi thô từ input.</param>
        /// <returns>Chuỗi đã trim, hoặc null khi rỗng/trắng.</returns>
        internal static string TrimToNull(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
