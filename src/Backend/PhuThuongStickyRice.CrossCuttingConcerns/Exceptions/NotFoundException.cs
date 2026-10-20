using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Exceptions
{
    /// <summary>
    /// Ngoại lệ chuẩn khi không tìm thấy tài nguyên theo id/mã (sản phẩm,
    /// đơn hàng, mã giảm giá...). Middleware xử lý lỗi ánh xạ loại này
    /// sang HTTP 404 kèm thông điệp thân thiện cho client.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException()
            : base()
        {
        }

        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
