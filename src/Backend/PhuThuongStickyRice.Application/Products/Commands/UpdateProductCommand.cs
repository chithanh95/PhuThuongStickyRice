using MediatR;
using PhuThuongStickyRice.Application.Products.DTOs;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Products.Commands
{
    public sealed class UpdateProductCommand : IRequest<ProductDto>
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; } = "VND";

        public string Status { get; set; }
    }

    internal sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDomainEvents _domainEvents;

        public UpdateProductCommandHandler(
            IProductRepository products,
            IUnitOfWork unitOfWork,
            IDomainEvents domainEvents)
        {
            _products = products;
            _unitOfWork = unitOfWork;
            _domainEvents = domainEvents;
        }

        public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _products.FirstOrDefaultAsync(_products.GetAll().Where(x => x.Id == request.Id));

            if (product == null)
            {
                throw new ProductNotFoundException(request.Id);
            }

            product.UpdateDetails(request.Name, request.Description);
            product.ChangePrice(Money.Of(request.Price, request.Currency));
            ApplyStatus(product, request.Status);

            using (await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
            {
                await _products.UpdateAsync(product, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }

            await _domainEvents.DispatchAsync(new EntityUpdatedEvent<Product>(product, DateTime.UtcNow), cancellationToken);

            return product.ToDto();
        }

        private static void ApplyStatus(Product product, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return;
            }

            if (!Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var parsed))
            {
                throw new Domain.Exceptions.DomainException($"Unknown product status '{status}'.");
            }

            switch (parsed)
            {
                case ProductStatus.Active:
                    product.Activate();
                    break;
                case ProductStatus.Inactive:
                    product.Deactivate();
                    break;
                case ProductStatus.Discontinued:
                    product.Discontinue();
                    break;
            }
        }
    }
}
