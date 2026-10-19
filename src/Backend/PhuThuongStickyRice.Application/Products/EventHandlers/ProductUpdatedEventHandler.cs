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
    /// <summary>
    /// Xử lý khi món ăn bị sửa: ghi AuditLogEntry ("UPDATED_PRODUCT") lưu
    /// snapshot món sau sửa + OutboxEvent ("PRODUCT_UPDATED") cho worker
    /// nền đẩy sang hệ thống ngoài. Cùng lý do với
    /// <see cref="ProductCreatedEventHandler"/>: không để tích hợp ngoài
    /// làm chậm/gãy luồng sửa món trong DB.
    /// </summary>
    public class ProductUpdatedEventHandler : IDomainEventHandler<EntityUpdatedEvent<Product>>
    {
        private readonly IEntityCrudService<AuditLogEntry> _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly IRepository<OutboxEvent, long> _outboxEventRepository;

        public ProductUpdatedEventHandler(IEntityCrudService<AuditLogEntry> auditService,
            ICurrentUser currentUser,
            IRepository<OutboxEvent, long> outboxEventRepository)
        {
            _auditService = auditService;
            _currentUser = currentUser;
            _outboxEventRepository = outboxEventRepository;
        }

        /// <summary>
        /// Ghi audit + xếp outbox cho món vừa sửa. Chạy trong UoW của thao
        /// tác sửa món nên cả 2 dấu vết được commit/rollback cùng món.
        /// </summary>
        /// <param name="domainEvent">Sự kiện sửa món (mang entity + thời điểm).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public async Task HandleAsync(EntityUpdatedEvent<Product> domainEvent, CancellationToken cancellationToken = default)
        {
            await _auditService.AddOrUpdateAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.UserId,
                CreatedDateTime = domainEvent.EventDateTime,
                Action = "UPDATED_PRODUCT",
                ObjectId = domainEvent.Entity.Id.ToString(),
                Log = domainEvent.Entity.AsJsonString(),
            }, cancellationToken);

            await _outboxEventRepository.AddOrUpdateAsync(new OutboxEvent
            {
                EventType = "PRODUCT_UPDATED",
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
