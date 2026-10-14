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
        public const int CustomerEmailMaxLength = 254;
        public const int CustomerAddressMaxLength = 500;
        public const int NotesMaxLength = 1000;

        public string OrderNumber { get; private set; }

        public string CustomerName { get; private set; }

        public string CustomerPhone { get; private set; }

        /// <summary>
        /// Optional. When provided, the order confirmation email is sent here.
        /// Customers without an email simply do not get an email.
        /// </summary>
        public string CustomerEmail { get; private set; }

        public string CustomerAddress { get; private set; }

        public string Notes { get; private set; }

        public OrderStatus Status { get; private set; }

        public PaymentMethod PaymentMethod { get; private set; }

        public PaymentStatus PaymentStatus { get; private set; }

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
            string customerEmail,
            string customerAddress,
            string notes,
            PaymentMethod paymentMethod,
            List<OrderLine> lines)
        {
            OrderNumber = orderNumber;
            CustomerName = customerName;
            CustomerPhone = customerPhone;
            CustomerEmail = customerEmail;
            CustomerAddress = customerAddress;
            Notes = notes;
            Status = OrderStatus.Pending;
            PaymentMethod = paymentMethod;
            PaymentStatus = PaymentStatus.Unpaid;
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
            string customerEmail,
            string customerAddress,
            string notes,
            PaymentMethod paymentMethod,
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

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var email = customerEmail.Trim();
                if (email.Length > CustomerEmailMaxLength || !IsValidEmail(email))
                {
                    throw new DomainException("Customer email is not a valid email address.");
                }
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
                string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
                customerAddress.Trim(),
                notes?.Trim(),
                paymentMethod,
                orderLines);
        }

        /// <summary>
        /// Minimal email shape check (the DTO layer also validates with
        /// <c>[EmailAddress]</c>). Kept dependency-free in the domain.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@'))
            {
                return false;
            }

            var dot = email.LastIndexOf('.');
            return dot > at + 1 && dot < email.Length - 1 && !email.Contains(" ");
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

        /// <summary>
        /// Customer reports they sent the bank transfer. Only for bank-transfer
        /// orders that are still unpaid. Admin confirms with <see cref="ConfirmPayment"/>.
        /// </summary>
        public void MarkTransferred()
        {
            if (PaymentMethod != PaymentMethod.BankTransfer)
            {
                throw new DomainException("Only bank-transfer orders can be marked as transferred.");
            }

            if (PaymentStatus != PaymentStatus.Unpaid)
            {
                throw new DomainException($"Cannot mark payment as transferred. Current payment status: '{PaymentStatus}'.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Cannot mark payment for a cancelled order.");
            }

            PaymentStatus = PaymentStatus.PendingConfirmation;
        }

        /// <summary>
        /// Payment confirmed — by the admin (bank transfer) or by the MoMo callback.
        /// Allowed from Unpaid (MoMo, COD collected) or PendingConfirmation (bank transfer).
        /// </summary>
        public void ConfirmPayment()
        {
            if (PaymentStatus == PaymentStatus.Paid)
            {
                throw new DomainException("Payment is already confirmed.");
            }

            if (PaymentStatus != PaymentStatus.Unpaid && PaymentStatus != PaymentStatus.PendingConfirmation)
            {
                throw new DomainException($"Cannot confirm payment. Current payment status: '{PaymentStatus}'.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Cannot confirm payment for a cancelled order.");
            }

            PaymentStatus = PaymentStatus.Paid;
        }

        /// <summary>
        /// Payment failed (MoMo reported failure). Allowed from Unpaid / PendingConfirmation.
        /// </summary>
        public void MarkPaymentFailed()
        {
            if (PaymentStatus == PaymentStatus.Paid)
            {
                throw new DomainException("A paid order cannot be marked as payment failed.");
            }

            if (PaymentStatus != PaymentStatus.Unpaid && PaymentStatus != PaymentStatus.PendingConfirmation)
            {
                throw new DomainException($"Cannot mark payment as failed. Current payment status: '{PaymentStatus}'.");
            }

            PaymentStatus = PaymentStatus.Failed;
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
