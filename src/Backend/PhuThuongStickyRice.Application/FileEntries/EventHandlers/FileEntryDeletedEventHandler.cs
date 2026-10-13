using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.FileEntries.EventHandlers
{
    public class FileEntryDeletedEventHandler : IDomainEventHandler<EntityDeletedEvent<FileEntry>>
    {
        private readonly ICrudService<AuditLogEntry> _auditSerivce;
        private readonly ICurrentUser _currentUser;
        private readonly IRepository<OutboxEvent, long> _outboxEventRepository;

        public FileEntryDeletedEventHandler(ICrudService<AuditLogEntry> auditSerivce,
            ICurrentUser currentUser,
            IRepository<OutboxEvent, long> outboxEventRepository)
        {
            _auditSerivce = auditSerivce;
            _currentUser = currentUser;
            _outboxEventRepository = outboxEventRepository;
        }

        public async Task HandleAsync(EntityDeletedEvent<FileEntry> domainEvent, CancellationToken cancellationToken = default)
        {
            await _auditSerivce.AddOrUpdateAsync(new AuditLogEntry
            {
                UserId = _currentUser.IsAuthenticated ? _currentUser.UserId : Guid.Empty,
                CreatedDateTime = domainEvent.EventDateTime,
                Action = "DELETE_FILEENTRY",
                ObjectId = domainEvent.Entity.Id.ToString(),
                Log = domainEvent.Entity.AsJsonString(),
            });

            await _outboxEventRepository.AddOrUpdateAsync(new OutboxEvent
            {
                EventType = "FILEENTRY_DELETED",
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
