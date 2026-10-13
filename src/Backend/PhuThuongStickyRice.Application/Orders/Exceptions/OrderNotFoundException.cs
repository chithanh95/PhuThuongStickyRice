using System;

namespace PhuThuongStickyRice.Application.Orders.Exceptions
{
    public class OrderNotFoundException : Exception
    {
        public OrderNotFoundException(Guid id)
            : base($"Order with id '{id}' was not found.")
        {
        }
    }
}
