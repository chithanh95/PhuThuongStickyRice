using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// A line item inside an <see cref="Order"/>. Belongs to the Order aggregate.
    /// </summary>
    public class OrderLine : Entity<Guid>
    {
        public Guid OrderId { get; private set; }

        public Guid ProductId { get; private set; }

        public string ProductName { get; private set; }

        public Money UnitPrice { get; private set; }

        public int Quantity { get; private set; }

        public Money LineTotal { get; private set; }

        // EF Core
        private OrderLine()
        {
        }

        private OrderLine(Guid productId, string productName, Money unitPrice, int quantity)
        {
            ProductId = productId;
            ProductName = productName;
            UnitPrice = unitPrice;
            Quantity = quantity;
            LineTotal = Money.Of(unitPrice.Amount * quantity, unitPrice.Currency);
        }

        internal static OrderLine Create(Guid productId, string productName, Money unitPrice, int quantity)
        {
            if (productId == Guid.Empty)
            {
                throw new DomainException("Order line must reference a product.");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new DomainException("Order line product name must not be empty.");
            }

            if (unitPrice == null)
            {
                throw new DomainException("Order line unit price is required.");
            }

            if (quantity < 1)
            {
                throw new DomainException("Order line quantity must be at least 1.");
            }

            return new OrderLine(productId, productName.Trim(), unitPrice, quantity);
        }
    }
}
