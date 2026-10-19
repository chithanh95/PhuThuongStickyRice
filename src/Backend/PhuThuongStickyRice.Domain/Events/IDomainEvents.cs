using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Events
{
    /// <summary>
    /// Cổng phát sự kiện domain — phía gọi chỉ cần interface này, không
    /// phụ thuộc implementation <see cref="DomainEvents"/> cụ thể.
    /// </summary>
    public interface IDomainEvents
    {
        /// <summary>Phát sự kiện tới mọi handler đã đăng ký cho loại sự kiện này.</summary>
        Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
