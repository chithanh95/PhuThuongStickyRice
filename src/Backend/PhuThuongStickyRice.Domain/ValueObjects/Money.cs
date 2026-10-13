using PhuThuongStickyRice.Domain.Exceptions;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.ValueObjects
{
    public class Money : ValueObject
    {
        public decimal Amount { get; private set; }

        public string Currency { get; private set; }

        private Money()
        {
        }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public static Money Of(decimal amount, string currency)
        {
            if (amount < 0)
            {
                throw new DomainException("Price amount must not be negative.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new DomainException("Currency must not be empty.");
            }

            var normalized = currency.Trim().ToUpperInvariant();

            if (normalized.Length != 3)
            {
                throw new DomainException("Currency must be a 3-letter ISO 4217 code (e.g. VND).");
            }

            return new Money(amount, normalized);
        }

        public static Money Zero(string currency)
        {
            return Of(0, currency);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString()
        {
            return $"{Amount} {Currency}";
        }
    }
}
