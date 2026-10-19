using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Command thêm-mới-hoặc-sửa 1 entity (Id rỗng thì thêm, có Id thì sửa) —
    /// xử lý bởi <c>AddOrUpdateEntityCommandHandler&lt;T&gt;</c>.
    /// </summary>
    /// <typeparam name="TEntity">Aggregate root cần thêm/sửa.</typeparam>
    public class AddOrUpdateEntityCommand<TEntity> : ICommand
        where TEntity : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Khởi tạo command thêm/sửa entity.</summary>
        /// <param name="entity">Entity cần thêm hoặc sửa.</param>
        public AddOrUpdateEntityCommand(TEntity entity)
        {
            Entity = entity;
        }

        public TEntity Entity { get; set; }
    }

    internal class AddOrUpdateEntityCommandHandler<TEntity> : ICommandHandler<AddOrUpdateEntityCommand<TEntity>>
    where TEntity : Entity<Guid>, IAggregateRoot
    {
        private readonly ICrudService<TEntity> _crudService;

        public AddOrUpdateEntityCommandHandler(ICrudService<TEntity> crudService)
        {
            _crudService = crudService;
        }

        public async Task HandleAsync(AddOrUpdateEntityCommand<TEntity> command, CancellationToken cancellationToken = default)
        {
            await _crudService.AddOrUpdateAsync(command.Entity, cancellationToken);
        }
    }
}
