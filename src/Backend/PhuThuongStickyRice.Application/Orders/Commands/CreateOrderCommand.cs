using MediatR;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Exceptions;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Orders.Commands
{
    public sealed class CreateOrderLineRequest
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }

    public sealed class CreateOrderCommand : IRequest<OrderDto>
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        public List<CreateOrderLineRequest> Lines { get; set; } = new List<CreateOrderLineRequest>();
    }

    internal sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDomainEvents _domainEvents;

        public CreateOrderCommandHandler(
            IOrderRepository orders,
            IProductRepository products,
            IUnitOfWork unitOfWork,
            IDomainEvents domainEvents)
        {
            _orders = orders;
            _products = products;
            _unitOfWork = unitOfWork;
            _domainEvents = domainEvents;
        }

        public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.Lines == null || request.Lines.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Order must contain at least one line.");
            }

            var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToList();
            var products = await _products.ToListAsync(
                _products.GetAll().Where(x => productIds.Contains(x.Id)));

            var lines = new List<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity)>();
            foreach (var line in request.Lines)
            {
                var product = products.FirstOrDefault(x => x.Id == line.ProductId);
                if (product == null)
                {
                    throw new Products.Exceptions.ProductNotFoundException(line.ProductId);
                }

                if (product.Status != ProductStatus.Active)
                {
                    throw new Domain.Exceptions.DomainException($"Product '{product.Name}' is not available for ordering.");
                }

                lines.Add((product.Id, product.Name, product.Price, line.Quantity));
            }

            var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

            var order = Order.Create(
                orderNumber,
                request.CustomerName,
                request.CustomerPhone,
                request.CustomerAddress,
                request.Notes,
                lines);

            using (await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
            {
                await _orders.AddAsync(order, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }

            await _domainEvents.DispatchAsync(new EntityCreatedEvent<Order>(order, DateTime.UtcNow), cancellationToken);

            return order.ToDto();
        }

        private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
        {
            var random = new Random();

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var orderNumber = $"PX-{DateTime.UtcNow:yyyyMMdd}-{random.Next(1000, 9999)}";

                if (!await _orders.ExistsWithOrderNumberAsync(orderNumber, cancellationToken))
                {
                    return orderNumber;
                }
            }

            throw new Domain.Exceptions.DomainException("Could not generate a unique order number. Please try again.");
        }
    }
}
