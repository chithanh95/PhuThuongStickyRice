using MediatR;
using PhuThuongStickyRice.Application.Products.DTOs;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Products.Queries
{
    public sealed class GetProductByIdQuery : IRequest<ProductDto>
    {
        public Guid Id { get; set; }
    }

    internal sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
    {
        private readonly IProductRepository _products;

        public GetProductByIdQueryHandler(IProductRepository products)
        {
            _products = products;
        }

        public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _products.FirstOrDefaultAsync(_products.GetAll().Where(x => x.Id == request.Id));

            if (product == null)
            {
                throw new ProductNotFoundException(request.Id);
            }

            return product.ToDto();
        }
    }
}
