using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Command thêm 1 entity mới — xử lý bởi
    /// <c>AddEntityCommandHandler&lt;T&gt;</c> qua <see cref="ICrudService{T}"/>.
    /// </summary>
    /// <typeparam name="TEntity">Aggregate root cần thêm.</typeparam>
    public class AddEntityCommand<TEntity> : ICommand
        where TEntity : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Khởi tạo command thêm entity mới.</summary>
        /// <param name="entity">Entity cần thêm vào DB.</param>
        public AddEntityCommand(TEntity entity)
        {
            Entity = entity;
        }

        public TEntity Entity { get; set; }
    }

    internal class AddEntityCommandHandler<TEntity> : ICommandHandler<AddEntityCommand<TEntity>>
    where TEntity : Entity<Guid>, IAggregateRoot
    {
        private readonly ICrudService<TEntity> _crudService;

        public AddEntityCommandHandler(ICrudService<TEntity> crudService)
        {
            _crudService = crudService;
        }

        public async Task HandleAsync(AddEntityCommand<TEntity> command, CancellationToken cancellationToken = default)
        {
            await _crudService.AddAsync(command.Entity, cancellationToken);
        }
    }
}
