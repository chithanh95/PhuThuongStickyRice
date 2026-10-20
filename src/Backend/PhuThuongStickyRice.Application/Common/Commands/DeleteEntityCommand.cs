using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Command xóa 1 entity — xử lý bởi
    /// <c>DeleteEntityCommandHandler&lt;T&gt;</c> qua <see cref="IEntityCrudService{T}"/>.
    /// </summary>
    /// <typeparam name="TEntity">Aggregate root cần xóa.</typeparam>
    public class DeleteEntityCommand<TEntity> : ICommand
         where TEntity : Entity<Guid>, IAggregateRoot
    {
        public TEntity Entity { get; set; }
    }

    internal sealed class DeleteEntityCommandHandler<TEntity> : ICommandHandler<DeleteEntityCommand<TEntity>>
    where TEntity : Entity<Guid>, IAggregateRoot
    {
        private readonly IEntityCrudService<TEntity> _crudService;

        public DeleteEntityCommandHandler(IEntityCrudService<TEntity> crudService)
        {
            _crudService = crudService;
        }

        public async Task HandleAsync(DeleteEntityCommand<TEntity> command, CancellationToken cancellationToken = default)
        {
            await _crudService.DeleteAsync(command.Entity, cancellationToken);
        }
    }
}
