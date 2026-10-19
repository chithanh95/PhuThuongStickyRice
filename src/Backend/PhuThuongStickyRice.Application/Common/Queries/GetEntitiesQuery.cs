using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    public class GetEntitiesQuery<TEntity> : IQuery<List<TEntity>>
         where TEntity : Entity<Guid>, IAggregateRoot
    {
    }

    internal class GetEntitiesQueryHandler<TEntity> : IQueryHandler<GetEntitiesQuery<TEntity>, List<TEntity>>
    where TEntity : Entity<Guid>, IAggregateRoot
    {
        private readonly IRepository<TEntity, Guid> _repository;

        public GetEntitiesQueryHandler(IRepository<TEntity, Guid> repository)
        {
            _repository = repository;
        }

        public async Task<List<TEntity>> HandleAsync(GetEntitiesQuery<TEntity> query, CancellationToken cancellationToken = default)
        {
            return await _repository.ToListAsync(_repository.GetAll());
        }
    }
}
