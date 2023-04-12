using PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers;
using System;

namespace PhuThuongStickyRice.Infrastructure.MessageBrokers.Fake
{
    public class FakeReceiver<T> : IMessageReceiver<T>
    {
        public void Receive(Action<T, MetaData> action)
        {
            // do nothing
        }
    }
}
