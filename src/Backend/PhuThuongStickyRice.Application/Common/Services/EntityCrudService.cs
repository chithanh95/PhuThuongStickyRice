using PhuThuongStickyRice.CrossCuttingConcerns.Exceptions;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// CRUD generic cho mọi aggregate root: thêm/sửa/xóa qua repository, lưu
    /// UnitOfWork rồi phát domain event tương ứng. Các handler command
    /// (Add/Update/Delete entity) đều dùng chung service này.
    /// </summary>
    /// <typeparam name="T">Aggregate root thao tác.</typeparam>
    public class EntityCrudService<T> : IEntityCrudService<T>
        where T : Entity<Guid>, IAggregateRoot
    {
        private readonly IUnitOfWork _unitOfWork;
        // CA1051: không để instance field visible (protected) khi không có
        // subclass nào dùng — chuyển về private. Giữ nguyên behavior.
        private readonly IRepository<T, Guid> _repository;
        private readonly IDomainEvents _domainEvents;

        /// <summary>
        /// Khởi tạo service CRUD với repository của entity và bus domain events.
        /// UnitOfWork lấy từ repository để 1 giao dịch bao hết thao tác + event.
        /// </summary>
        public EntityCrudService(IRepository<T, Guid> repository, IDomainEvents domainEvents)
        {
            _unitOfWork = repository.UnitOfWork;
            _repository = repository;
            _domainEvents = domainEvents;
        }

        public Task<List<T>> GetAsync(CancellationToken cancellationToken = default)
        {
            return _repository.ToListAsync(_repository.GetAll());
        }

        public Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            ValidationException.Requires(id != Guid.Empty, "Invalid Id");
            return _repository.FirstOrDefaultAsync(_repository.GetAll().Where(x => x.Id == id));
        }

        /// <summary>
        /// Thêm hoặc sửa 1 entity: Id rỗng (Guid.Empty) coi như entity mới
        /// cần thêm, ngược lại là sửa entity đã có. Phát event tương ứng.
        /// </summary>
        public async Task AddOrUpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity.Id.Equals(default))
            {
                await AddAsync(entity, cancellationToken);
            }
            else
            {
                await UpdateAsync(entity, cancellationToken);
            }
        }

        /// <summary>
        /// Thêm entity mới: lưu DB rồi phát <see cref="EntityCreatedEvent{T}"/>
        /// (handler audit log dùng để ghi lịch sử tạo).
        /// </summary>
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _domainEvents.DispatchAsync(new EntityCreatedEvent<T>(entity, DateTime.UtcNow), cancellationToken);
        }

        /// <summary>
        /// Sửa entity: lưu DB rồi phát <see cref="EntityUpdatedEvent{T}"/>
        /// (handler audit log dùng để ghi lịch sử sửa).
        /// </summary>
        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _domainEvents.DispatchAsync(new EntityUpdatedEvent<T>(entity, DateTime.UtcNow), cancellationToken);
        }

        /// <summary>
        /// Xóa entity: lưu DB rồi phát <see cref="EntityDeletedEvent{T}"/>
        /// (handler audit log dùng để ghi lịch sử xóa).
        /// </summary>
        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _domainEvents.DispatchAsync(new EntityDeletedEvent<T>(entity, DateTime.UtcNow), cancellationToken);
        }
    }
}
