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
    /// Xử lý khi món ăn được tạo: ghi 2 dấu vết trong cùng 1 UoW —
    /// (1) AuditLogEntry ("CREATED_PRODUCT") để admin tra được ai tạo món,
    /// lúc nào, dữ liệu món ra sao; (2) OutboxEvent ("PRODUCT_CREATED",
    /// Published = false) để worker nền đẩy dần sang hệ thống ngoài
    /// (webhook/Facebook...) mà không chặn luồng tạo món. Dùng outbox thay
    /// vì gọi trực tiếp vì hệ thống ngoài có thể chậm/hỏng — tạo món trong
    /// DB phải thành công kể cả khi tích hợp ngoài đang lỗi.
    /// </summary>
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

        /// <summary>
        /// Ghi audit + xếp outbox cho món vừa tạo. Chạy trong UoW của thao
        /// tác tạo món nên cả 2 dấu vết được commit/rollback cùng món.
        /// </summary>
        /// <param name="domainEvent">Sự kiện tạo món (mang entity + thời điểm).</param>
        /// <param name="cancellationToken">Token hủy.</param>
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

            await _outboxEventRepository.AddOrUpdateAsync(ProductOutboxFactory.Create(
                "PRODUCT_CREATED", _currentUser, domainEvent.Entity, domainEvent.EventDateTime), cancellationToken);

            await _outboxEventRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
