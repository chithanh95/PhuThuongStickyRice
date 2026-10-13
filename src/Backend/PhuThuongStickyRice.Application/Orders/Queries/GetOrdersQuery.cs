using MediatR;
using PhuThuongStickyRice.Application.Common.DTOs;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Exceptions;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Orders.Queries
{
    public sealed class GetOrdersQuery : IRequest<Paged<OrderDto>>
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SearchTerm { get; set; }

        public string Status { get; set; }
    }

    internal sealed class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Paged<OrderDto>>
    {
        private readonly IOrderRepository _orders;

        public GetOrdersQueryHandler(IOrderRepository orders)
        {
            _orders = orders;
        }

        public async Task<Paged<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            IQueryable<Order> query = _orders.GetAll().OrderByDescending(x => x.CreatedAt);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(x =>
                    x.OrderNumber.Contains(term)
                    || x.CustomerName.Contains(term)
                    || x.CustomerPhone.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(request.Status)
                && Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
            {
                query = query.Where(x => x.Status == status);
            }

            var totalItems = await _orders.CountAsync(query, cancellationToken);

            var items = await _orders.ToListAsync(
                query.Skip((page - 1) * pageSize).Take(pageSize));

            var dtos = new List<OrderDto>();
            foreach (var item in items)
            {
                dtos.Add(item.ToDto());
            }

            return new Paged<OrderDto>
            {
                TotalItems = totalItems,
                Items = dtos
            };
        }
    }

    public sealed class GetOrderByIdQuery : IRequest<OrderDto>
    {
        public Guid OrderId { get; set; }
    }

    internal sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
    {
        private readonly IOrderRepository _orders;

        public GetOrderByIdQueryHandler(IOrderRepository orders)
        {
            _orders = orders;
        }

        public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _orders.FirstOrDefaultAsync(
                _orders.GetAll().Where(x => x.Id == request.OrderId));

            if (order == null)
            {
                throw new OrderNotFoundException(request.OrderId);
            }

            return order.ToDto();
        }
    }
}
