using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Events
{
    /// <summary>
    /// Xử lý 1 loại sự kiện domain cụ thể. Implement interface này để
    /// <see cref="DomainEvents.RegisterHandlers"/> tự phát hiện và gọi khi
    /// sự kiện được dispatch.
    /// </summary>
    /// <typeparam name="T">Loại sự kiện mà handler này xử lý.</typeparam>
    public interface IDomainEventHandler<T>
           where T : IDomainEvent
    {
        /// <summary>Xử lý 1 sự kiện domain.</summary>
        Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default);
    }
}
