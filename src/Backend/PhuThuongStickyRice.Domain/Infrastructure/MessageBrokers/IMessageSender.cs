using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers
{
    /// <summary>
    /// Gửi message tới message broker (outbox/notification). Dùng pattern
    /// outbox: message được lưu DB trước, worker gửi sau — đảm bảo không mất
    /// thông báo khi app crash giữa chừng.
    /// </summary>
    /// <typeparam name="T">Loại message gửi đi.</typeparam>
    public interface IMessageSender<T>
    {
        /// <summary>
        /// Gửi một message tới broker.
        /// </summary>
        /// <param name="message">Nội dung message.</param>
        /// <param name="metadata">Metadata kèm theo (null = mặc định).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task SendAsync(T message, MessageMetadata metadata = null, CancellationToken cancellationToken = default);
    }
}
