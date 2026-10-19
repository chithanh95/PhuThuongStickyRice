using System;

namespace PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers
{
    public interface IMessageReceiver<T>
    {
        void Receive(Action<T, MessageMetadata> action);
    }
}
