using PhuThuongStickyRice.Domain.Entities;
using System.Linq;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    public static class OrderMappings
    {
        public static OrderDto ToDto(this Order order)
        {
            if (order == null)
            {
                return null;
            }

            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerAddress = order.CustomerAddress,
                Notes = order.Notes,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount.Amount,
                Currency = order.TotalAmount.Currency,
                CreatedAt = order.CreatedAt,
                Lines = order.Lines
                    .Select(x => new OrderLineDto
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        UnitPrice = x.UnitPrice.Amount,
                        Quantity = x.Quantity,
                        LineTotal = x.LineTotal.Amount
                    })
                    .ToList()
            };
        }
    }
}
