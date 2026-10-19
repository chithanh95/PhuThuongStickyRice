using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Domain.Events
{
    /// <summary>
    /// Sự kiện "entity vừa bị xóa" — phát sau khi aggregate bị xóa, kèm
    /// entity (snapshot trước khi xóa) và thời điểm xảy ra (UTC).
    /// </summary>
    /// <typeparam name="T">Loại entity bị xóa.</typeparam>
    public class EntityDeletedEvent<T> : IDomainEvent
        where T : Entity<Guid>
    {
        public EntityDeletedEvent(T entity, DateTime eventDateTime)
        {
            Entity = entity;
            EventDateTime = eventDateTime;
        }

        public T Entity { get; }

        public DateTime EventDateTime { get; }
    }
}
