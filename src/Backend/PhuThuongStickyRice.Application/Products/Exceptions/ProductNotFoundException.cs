using PhuThuongStickyRice.CrossCuttingConcerns.Exceptions;
using System;

namespace PhuThuongStickyRice.Application.Products.Exceptions
{
    /// <summary>
    /// Ngoại lệ ném ra khi truy vấn sản phẩm theo Id nhưng không tồn tại.
    /// Kế thừa <see cref="NotFoundException"/> nên middleware sẽ tự chuyển
    /// thành HTTP 404 cho client.
    /// </summary>
    public class ProductNotFoundException : NotFoundException
    {
        /// <summary>
        /// Khởi tạo ngoại lệ với Id sản phẩm không tìm thấy.
        /// </summary>
        /// <param name="id">Id của sản phẩm cần tìm.</param>
        public ProductNotFoundException(Guid id)
            : base($"Product with id '{id}' was not found.")
        {
        }
    }
}
