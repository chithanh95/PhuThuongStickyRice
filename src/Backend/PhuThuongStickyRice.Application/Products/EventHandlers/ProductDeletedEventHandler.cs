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
    /// Xử lý khi món ăn bị xóa: ghi AuditLogEntry ("DELETED_PRODUCT") lưu
    /// snapshot món lần cuối (món đã mất khỏi DB nên đây là dấu vết duy
    /// nhất để đối soát sau này) + OutboxEvent ("PRODUCT_DELETED") cho
    /// worker nền. Cùng lý do với <see cref="ProductCreatedEventHandler"/>:
    /// không để tích hợp ngoài làm chậm/gãy luồng xóa món trong DB.
    /// </summary>
    public class ProductDeletedEventHandler : IDomainEventHandler<EntityDeletedEvent<Product>>
    {
        private readonly IEntityCrudService<AuditLogEntry> _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly IRepository<OutboxEvent, long> _outboxEventRepository;

        public ProductDeletedEventHandler(IEntityCrudService<AuditLogEntry> auditService,
            ICurrentUser currentUser,
            IRepository<OutboxEvent, long> outboxEventRepository)
        {
            _auditService = auditService;
            _currentUser = currentUser;
            _outboxEventRepository = outboxEventRepository;
        }

        /// <summary>
        /// Ghi audit + xếp outbox cho món vừa xóa. Chạy trong UoW của thao
        /// tác xóa món nên cả 2 dấu vết được commit/rollback cùng món.
        /// </summary>
        /// <param name="domainEvent">Sự kiện xóa món (mang entity + thời điểm).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public async Task HandleAsync(EntityDeletedEvent<Product> domainEvent, CancellationToken cancellationToken = default)
        {
            await _auditService.AddOrUpdateAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.UserId,
                CreatedDateTime = domainEvent.EventDateTime,
                Action = "DELETED_PRODUCT",
                ObjectId = domainEvent.Entity.Id.ToString(),
                Log = domainEvent.Entity.AsJsonString(),
            }, cancellationToken);

            await _outboxEventRepository.AddOrUpdateAsync(ProductOutboxFactory.Create(
                "PRODUCT_DELETED", _currentUser, domainEvent.Entity, domainEvent.EventDateTime), cancellationToken);

            await _outboxEventRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
