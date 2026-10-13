using MediatR;
using PhuThuongStickyRice.Application.Common.DTOs;
using PhuThuongStickyRice.Application.Products.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Products.Queries
{
    public sealed class GetProductsQuery : IRequest<Paged<ProductDto>>
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SearchTerm { get; set; }

        public string Status { get; set; }
    }

    internal sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Paged<ProductDto>>
    {
        private readonly IProductRepository _products;

        public GetProductsQueryHandler(IProductRepository products)
        {
            _products = products;
        }

        public async Task<Paged<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            IQueryable<Product> query = _products.GetAll().OrderBy(x => x.Name);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(x => x.Name.Contains(term) || x.Code.Value.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(request.Status)
                && Enum.TryParse<ProductStatus>(request.Status, ignoreCase: true, out var status))
            {
                query = query.Where(x => x.Status == status);
            }

            var totalItems = await _products.CountAsync(query, cancellationToken);

            var items = await _products.ToListAsync(
                query.Skip((page - 1) * pageSize).Take(pageSize));

            var dtos = new List<ProductDto>();
            foreach (var item in items)
            {
                dtos.Add(item.ToDto());
            }

            return new Paged<ProductDto>
            {
                TotalItems = totalItems,
                Items = dtos
            };
        }
    }
}
