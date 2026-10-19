using System;

namespace PhuThuongStickyRice.Application.Products.Exceptions
{
    /// <summary>
    /// Ngoại lệ nghiệp vụ ném ra khi tạo/cập nhật sản phẩm với mã (Code)
    /// đã tồn tại trong hệ thống. Mã sản phẩm phải duy nhất nên thao tác
    /// bị chặn để tránh trùng dữ liệu.
    /// </summary>
    public class DuplicateProductCodeException : Exception
    {
        /// <summary>
        /// Khởi tạo ngoại lệ với mã sản phẩm bị trùng.
        /// </summary>
        /// <param name="code">Mã sản phẩm đã tồn tại.</param>
        public DuplicateProductCodeException(string code)
            : base($"A product with code '{code}' already exists.")
        {
        }
    }
}
