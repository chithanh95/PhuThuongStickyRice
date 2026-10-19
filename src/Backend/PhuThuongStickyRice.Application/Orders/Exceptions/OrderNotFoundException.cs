using PhuThuongStickyRice.CrossCuttingConcerns.Exceptions;
using System;

namespace PhuThuongStickyRice.Application.Orders.Exceptions
{
    /// <summary>
    /// Ngoại lệ ném ra khi truy vấn đơn hàng nhưng không tồn tại.
    /// Kế thừa <see cref="NotFoundException"/> nên middleware sẽ tự chuyển
    /// thành HTTP 404 cho client.
    /// </summary>
    public class OrderNotFoundException : NotFoundException
    {
        /// <summary>
        /// Khởi tạo ngoại lệ với Id đơn hàng không tìm thấy.
        /// </summary>
        /// <param name="id">Id của đơn hàng cần tìm.</param>
        public OrderNotFoundException(Guid id)
            : base($"Order with id '{id}' was not found.")
        {
        }

        /// <summary>
        /// Khởi tạo ngoại lệ với thông báo tùy chỉnh (dùng khi tra cứu
        /// đơn bằng số điện thoại/mã đơn thay vì Id).
        /// </summary>
        /// <param name="message">Thông báo lỗi hiển thị cho client.</param>
        public OrderNotFoundException(string message)
            : base(message)
        {
        }
    }
}
