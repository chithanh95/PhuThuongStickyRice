using MediatR;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Products.Commands
{
    public sealed class DeleteProductCommand : IRequest
    {
        public Guid Id { get; set; }
    }

    internal sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDomainEvents _domainEvents;

        public DeleteProductCommandHandler(
            IProductRepository products,
            IUnitOfWork unitOfWork,
            IDomainEvents domainEvents)
        {
            _products = products;
            _unitOfWork = unitOfWork;
            _domainEvents = domainEvents;
        }

        public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _products.FirstOrDefaultAsync(_products.GetAll().Where(x => x.Id == request.Id));

            if (product == null)
            {
                throw new ProductNotFoundException(request.Id);
            }

            using (await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
            {
                _products.Delete(product);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }

            await _domainEvents.DispatchAsync(new EntityDeletedEvent<Product>(product, DateTime.UtcNow), cancellationToken);
        }
    }
}
