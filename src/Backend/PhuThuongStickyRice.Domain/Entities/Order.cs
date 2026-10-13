using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Customer order aggregate root. All state changes go through
    /// intention-revealing behaviors so domain invariants are enforced in one place.
    /// </summary>
    public class Order : Entity<Guid>, IAggregateRoot
    {
        public const int OrderNumberMaxLength = 30;
        public const int CustomerNameMaxLength = 200;
        public const int CustomerPhoneMaxLength = 30;
        public const int CustomerAddressMaxLength = 500;
        public const int NotesMaxLength = 1000;

        public string OrderNumber { get; private set; }

        public string CustomerName { get; private set; }

        public string CustomerPhone { get; private set; }

        public string CustomerAddress { get; private set; }

        public string Notes { get; private set; }

        public OrderStatus Status { get; private set; }

        public Money TotalAmount { get; private set; }

        public DateTime CreatedAt { get; private set; }

        private readonly List<OrderLine> _lines = new List<OrderLine>();

        public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

        // EF Core
        private Order()
        {
        }

        private Order(
            string orderNumber,
            string customerName,
            string customerPhone,
            string customerAddress,
            string notes,
            List<OrderLine> lines)
        {
            OrderNumber = orderNumber;
            CustomerName = customerName;
            CustomerPhone = customerPhone;
            CustomerAddress = customerAddress;
            Notes = notes;
            Status = OrderStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            _lines.AddRange(lines);
            TotalAmount = Money.Of(
                lines.Sum(x => x.LineTotal.Amount),
                lines.First().LineTotal.Currency);
        }

        public static Order Create(
            string orderNumber,
            string customerName,
            string customerPhone,
            string customerAddress,
            string notes,
            IEnumerable<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity)> lines)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                throw new DomainException("Order number is required.");
            }

            if (orderNumber.Trim().Length > OrderNumberMaxLength)
            {
                throw new DomainException($"Order number must not exceed {OrderNumberMaxLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new DomainException("Customer name must not be empty.");
            }

            if (customerName.Trim().Length > CustomerNameMaxLength)
            {
                throw new DomainException($"Customer name must not exceed {CustomerNameMaxLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(customerPhone))
            {
                throw new DomainException("Customer phone must not be empty.");
            }

            if (customerPhone.Trim().Length > CustomerPhoneMaxLength)
            {
                throw new DomainException($"Customer phone must not exceed {CustomerPhoneMaxLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(customerAddress))
            {
                throw new DomainException("Customer address must not be empty.");
            }

            if (customerAddress.Trim().Length > CustomerAddressMaxLength)
            {
                throw new DomainException($"Customer address must not exceed {CustomerAddressMaxLength} characters.");
            }

            if (notes != null && notes.Length > NotesMaxLength)
            {
                throw new DomainException($"Order notes must not exceed {NotesMaxLength} characters.");
            }

            var orderLines = (lines ?? Enumerable.Empty<(Guid, string, Money, int)>())
                .Select(x => OrderLine.Create(x.ProductId, x.ProductName, x.UnitPrice, x.Quantity))
                .ToList();

            if (orderLines.Count == 0)
            {
                throw new DomainException("Order must contain at least one line.");
            }

            var currency = orderLines.First().UnitPrice.Currency;
            if (orderLines.Any(x => x.UnitPrice.Currency != currency))
            {
                throw new DomainException("All order lines must use the same currency.");
            }

            return new Order(
                orderNumber.Trim(),
                customerName.Trim(),
                customerPhone.Trim(),
                customerAddress.Trim(),
                notes?.Trim(),
                orderLines);
        }

        public void Confirm()
        {
            EnsureTransition(OrderStatus.Pending, nameof(Confirm));
            Status = OrderStatus.Confirmed;
        }

        public void StartPreparing()
        {
            EnsureTransition(OrderStatus.Confirmed, nameof(StartPreparing));
            Status = OrderStatus.Preparing;
        }

        public void StartDelivering()
        {
            EnsureTransition(OrderStatus.Preparing, nameof(StartDelivering));
            Status = OrderStatus.Delivering;
        }

        public void Complete()
        {
            EnsureTransition(OrderStatus.Delivering, nameof(Complete));
            Status = OrderStatus.Completed;
        }

        public void Cancel()
        {
            if (Status == OrderStatus.Completed)
            {
                throw new DomainException("A completed order cannot be cancelled.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Order is already cancelled.");
            }

            Status = OrderStatus.Cancelled;
        }

        private void EnsureTransition(OrderStatus expectedStatus, string action)
        {
            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException($"Cannot {action} a cancelled order.");
            }

            if (Status != expectedStatus)
            {
                throw new DomainException($"Order must be in '{expectedStatus}' status to {action}. Current status: '{Status}'.");
            }
        }
    }
}
