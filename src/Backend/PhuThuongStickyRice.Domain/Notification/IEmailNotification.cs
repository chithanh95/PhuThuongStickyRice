using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Notification
{
    /// <summary>
    /// Abstraction gửi email của domain — tách code domain khỏi nhà cung cấp
    /// cụ thể (SendGrid...), triển khai thật nằm ở tầng Infrastructure.
    /// </summary>
    public interface IEmailNotification
    {
        /// <summary>Gửi 1 email.</summary>
        /// <param name="emailMessage">Nội dung và người nhận email.</param>
        /// <param name="cancellationToken">Token hủy thao tác gửi.</param>
        Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default);
    }

    /// <summary>Dữ liệu 1 email cần gửi.</summary>
    public interface IEmailMessage
    {
        /// <summary>Địa chỉ người gửi.</summary>
        public string From { get; set; }

        /// <summary>Danh sách người nhận chính.</summary>
        public string Tos { get; set; }

        /// <summary>Danh sách người nhận CC.</summary>
        public string CCs { get; set; }

        /// <summary>Danh sách người nhận BCC (ẩn).</summary>
        public string BCCs { get; set; }

        /// <summary>Tiêu đề email.</summary>
        public string Subject { get; set; }

        /// <summary>Nội dung email.</summary>
        public string Body { get; set; }
    }
}
