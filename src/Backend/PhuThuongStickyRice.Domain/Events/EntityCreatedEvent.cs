using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Domain.Events
{
    /// <summary>
    /// Sự kiện "entity vừa được tạo" — phát sau khi aggregate mới được
    /// lưu, kèm entity và thời điểm xảy ra (UTC).
    /// </summary>
    /// <typeparam name="T">Loại entity được tạo.</typeparam>
    public class EntityCreatedEvent<T> : IDomainEvent
        where T : Entity<Guid>
    {
        public EntityCreatedEvent(T entity, DateTime eventDateTime)
        {
            Entity = entity;
            EventDateTime = eventDateTime;
        }

        public T Entity { get; }

        public DateTime EventDateTime { get; }
    }
}
