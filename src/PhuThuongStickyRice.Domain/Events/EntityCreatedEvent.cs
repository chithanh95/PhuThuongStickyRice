﻿using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Domain.Events
{
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
