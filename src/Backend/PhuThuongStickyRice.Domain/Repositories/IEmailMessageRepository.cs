using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// Repository cho hàng đợi email gửi đi (<see cref="EmailMessage"/>):
    /// email được lưu vào DB trước rồi BackgroundServer lấy ra gửi dần và thử
    /// lại khi lỗi, nên repository này là điểm truy xuất danh sách email chờ/xịt.
    /// </summary>
    public interface IEmailMessageRepository : IRepository<EmailMessage, Guid>
    {
    }
}
