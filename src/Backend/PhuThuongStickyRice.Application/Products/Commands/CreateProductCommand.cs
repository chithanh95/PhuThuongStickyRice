using MediatR;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Products.Commands
{
    public sealed class CreateProductCommand : IRequest<Guid>
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; } = "VND";
    }

    internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDomainEvents _domainEvents;

        public CreateProductCommandHandler(
            IProductRepository products,
            IUnitOfWork unitOfWork,
            IDomainEvents domainEvents)
        {
            _products = products;
            _unitOfWork = unitOfWork;
            _domainEvents = domainEvents;
        }

        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var code = ProductCode.Of(request.Code);

            if (await _products.ExistsWithCodeAsync(code, null, cancellationToken))
            {
                throw new DuplicateProductCodeException(code.Value);
            }

            var product = Product.Create(code, request.Name, Money.Of(request.Price, request.Currency), request.Description);

            using (await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
            {
                await _products.AddAsync(product, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }

            await _domainEvents.DispatchAsync(new EntityCreatedEvent<Product>(product, DateTime.UtcNow), cancellationToken);

            return product.Id;
        }
    }
}
