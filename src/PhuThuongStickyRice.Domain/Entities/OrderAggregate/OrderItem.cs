using System;

namespace PhuThuongStickyRice.Domain.Entities.OrderAggregate
{
    public class OrderItem : Entity<Guid>, IAggregateRoot
    {
        public decimal UnitPrice { get; private set; }
        public int Units { get; private set; }

#pragma warning disable CS8618 // Required by Entity Framework
        private OrderItem() { }

        public OrderItem(decimal unitPrice, int units)
        {
            UnitPrice = unitPrice;
            Units = units;
        }
    }
}
