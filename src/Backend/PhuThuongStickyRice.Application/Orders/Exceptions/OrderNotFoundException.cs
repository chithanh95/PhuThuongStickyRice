using PhuThuongStickyRice.CrossCuttingConcerns.Exceptions;
using System;

namespace PhuThuongStickyRice.Application.Orders.Exceptions
{
    public class OrderNotFoundException : NotFoundException
    {
        public OrderNotFoundException(Guid id)
            : base($"Order with id '{id}' was not found.")
        {
        }
    }
}
