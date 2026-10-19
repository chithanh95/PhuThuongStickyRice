using System;
﻿using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Products.EventHandlers
{
    public class ProductCreatedEventHandler : IDomainEventHandler<EntityCreatedEvent<Product>>
    {
        private readonly IEntityCrudService<AuditLogEntry> _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly IRepository<OutboxEvent, long> _outboxEventRepository;

        public ProductCreatedEventHandler(IEntityCrudService<AuditLogEntry> auditService,
            ICurrentUser currentUser,
            IRepository<OutboxEvent, long> outboxEventRepository)
        {
            _auditService = auditService;
            _currentUser = currentUser;
            _outboxEventRepository = outboxEventRepository;
        }

        public async Task HandleAsync(EntityCreatedEvent<Product> domainEvent, CancellationToken cancellationToken = default)
        {
            await _auditService.AddOrUpdateAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.IsAuthenticated ? _currentUser.UserId : Guid.Empty,
                CreatedDateTime = domainEvent.EventDateTime,
                Action = "CREATED_PRODUCT",
                ObjectId = domainEvent.Entity.Id.ToString(),
                Log = domainEvent.Entity.AsJsonString(),
            }, cancellationToken);

            await _outboxEventRepository.AddOrUpdateAsync(new OutboxEvent
            {
                EventType = "PRODUCT_CREATED",
                TriggeredById = _currentUser.UserId,
                CreatedDateTime = domainEvent.EventDateTime,
                ObjectId = domainEvent.Entity.Id.ToString(),
                Message = domainEvent.Entity.AsJsonString(),
                Published = false,
            }, cancellationToken);

            await _outboxEventRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
