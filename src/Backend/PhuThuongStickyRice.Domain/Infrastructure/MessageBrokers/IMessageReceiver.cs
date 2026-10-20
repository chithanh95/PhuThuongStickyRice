using System;

namespace PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers
{
    /// <summary>
    /// Nhận message từ broker: đăng ký một handler được gọi mỗi khi có
    /// message loại <typeparamref name="T"/> đến (dùng cho worker outbox).
    /// </summary>
    /// <typeparam name="T">Loại message nhận về.</typeparam>
    public interface IMessageReceiver<T>
    {
        /// <summary>
        /// Đăng ký hàm xử lý cho mỗi message nhận được.
        /// </summary>
        /// <param name="action">Hàm xử lý (message, metadata).</param>
        void Receive(Action<T, MessageMetadata> action);
    }
}
