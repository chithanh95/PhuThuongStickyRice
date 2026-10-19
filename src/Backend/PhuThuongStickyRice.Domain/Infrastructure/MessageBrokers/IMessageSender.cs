using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers
{
    public interface IMessageSender<T>
    {
        Task SendAsync(T message, MessageMetadata metadata = null, CancellationToken cancellationToken = default);
    }
}
