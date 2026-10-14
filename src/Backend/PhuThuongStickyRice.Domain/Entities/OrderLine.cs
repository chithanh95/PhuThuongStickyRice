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
        public const int SizeNameMaxLength = 100;
        public const int ToppingNamesMaxLength = 500;
        public const int NoteMaxLength = 500;

        public Guid OrderId { get; private set; }

        /// <summary>
        /// Product id — or the combo id when <see cref="IsCombo"/> is true.
        /// There is no FK constraint so combos (which are not products)
        /// can be referenced here.
        /// </summary>
        public Guid ProductId { get; private set; }

        public string ProductName { get; private set; }

        public Money UnitPrice { get; private set; }

        public int Quantity { get; private set; }

        public Money LineTotal { get; private set; }

        /// <summary>Chosen size option name (Nhỏ / Vừa / Lớn), if any.</summary>
        public string SizeName { get; private set; }

        /// <summary>Chosen topping names, comma-separated, if any.</summary>
        public string ToppingNames { get; private set; }

        /// <summary>Total topping surcharge for one unit, in the line currency.</summary>
        public decimal ToppingsExtra { get; private set; }

        /// <summary>True when this line is a family combo, not a product.</summary>
        public bool IsCombo { get; private set; }

        /// <summary>
        /// Per-item note from the customer (e.g. "ít hành", "không ớt").
        /// Rendered on the kitchen sticky labels when present.
        /// </summary>
        public string Note { get; private set; }

        // EF Core
        private OrderLine()
        {
        }

        private OrderLine(
            Guid productId,
            string productName,
            Money unitPrice,
            int quantity,
            string sizeName,
            string toppingNames,
            decimal toppingsExtra,
            bool isCombo,
            string note = null)
        {
            ProductId = productId;
            ProductName = productName;
            UnitPrice = unitPrice;
            Quantity = quantity;
            LineTotal = Money.Of(unitPrice.Amount * quantity, unitPrice.Currency);
            SizeName = sizeName;
            ToppingNames = toppingNames;
            ToppingsExtra = toppingsExtra;
            IsCombo = isCombo;
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }

        internal static OrderLine Create(
            Guid productId,
            string productName,
            Money unitPrice,
            int quantity,
            string sizeName = null,
            string toppingNames = null,
            decimal toppingsExtra = 0,
            bool isCombo = false,
            string note = null)
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

            if (sizeName != null && sizeName.Length > SizeNameMaxLength)
            {
                throw new DomainException($"Size name must not exceed {SizeNameMaxLength} characters.");
            }

            if (toppingNames != null && toppingNames.Length > ToppingNamesMaxLength)
            {
                throw new DomainException($"Topping names must not exceed {ToppingNamesMaxLength} characters.");
            }

            if (toppingsExtra < 0)
            {
                throw new DomainException("Toppings extra must not be negative.");
            }

            if (note != null && note.Length > NoteMaxLength)
            {
                throw new DomainException($"Order line note must not exceed {NoteMaxLength} characters.");
            }

            return new OrderLine(
                productId,
                productName.Trim(),
                unitPrice,
                quantity,
                string.IsNullOrWhiteSpace(sizeName) ? null : sizeName.Trim(),
                string.IsNullOrWhiteSpace(toppingNames) ? null : toppingNames.Trim(),
                toppingsExtra,
                isCombo,
                note);
        }

        /// <summary>
        /// Quantity edit by the customer on a Pending order (0 = remove line).
        /// Called from the Order aggregate root only.
        /// </summary>
        internal void SetQuantity(int quantity)
        {
            if (quantity < 0)
            {
                throw new DomainException("Order line quantity cannot be negative.");
            }

            Quantity = quantity;
            LineTotal = Money.Of(UnitPrice.Amount * quantity, UnitPrice.Currency);
        }
    }
}
