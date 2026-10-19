using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Domain.Events
{
    /// <summary>
    /// Sự kiện "entity vừa được cập nhật" — phát sau khi aggregate thay đổi
    /// được lưu, kèm entity và thời điểm xảy ra (UTC).
    /// </summary>
    /// <typeparam name="T">Loại entity được cập nhật.</typeparam>
    public class EntityUpdatedEvent<T> : IDomainEvent
        where T : Entity<Guid>
    {
        public EntityUpdatedEvent(T entity, DateTime eventDateTime)
        {
            Entity = entity;
            EventDateTime = eventDateTime;
        }

        public T Entity { get; }

        public DateTime EventDateTime { get; }
    }
}
