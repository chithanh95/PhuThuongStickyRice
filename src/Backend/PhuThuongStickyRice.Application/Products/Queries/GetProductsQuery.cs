using PhuThuongStickyRice.Application.Decorators.AuditLog;
using PhuThuongStickyRice.Application.Decorators.DatabaseRetry;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Products.Queries
{
    public class GetProductsQuery : IQuery<List<Product>>
    {
    }

    [AuditLog]
    [DatabaseRetry]
    internal class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, List<Product>>
    {
        private readonly IRepository<Product, Guid> _productRepository;

        public GetProductsQueryHandler(IRepository<Product, Guid> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<Product>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken = default)
        {
            return await _productRepository.ToListAsync(_productRepository.GetAll());
        }
    }
}
