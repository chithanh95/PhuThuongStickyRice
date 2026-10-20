using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Command sửa 1 entity đã có — xử lý bởi
    /// <c>UpdateEntityCommandHandler&lt;T&gt;</c> qua <see cref="IEntityCrudService{T}"/>.
    /// </summary>
    /// <typeparam name="TEntity">Aggregate root cần sửa.</typeparam>
    public class UpdateEntityCommand<TEntity> : ICommand
        where TEntity : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Khởi tạo command sửa entity.</summary>
        /// <param name="entity">Entity với dữ liệu mới.</param>
        public UpdateEntityCommand(TEntity entity)
        {
            Entity = entity;
        }

        public TEntity Entity { get; set; }
    }

    internal sealed class UpdateEntityCommandHandler<TEntity> : ICommandHandler<UpdateEntityCommand<TEntity>>
    where TEntity : Entity<Guid>, IAggregateRoot
    {
        private readonly IEntityCrudService<TEntity> _crudService;

        public UpdateEntityCommandHandler(IEntityCrudService<TEntity> crudService)
        {
            _crudService = crudService;
        }

        public async Task HandleAsync(UpdateEntityCommand<TEntity> command, CancellationToken cancellationToken = default)
        {
            await _crudService.UpdateAsync(command.Entity, cancellationToken);
        }
    }
}
