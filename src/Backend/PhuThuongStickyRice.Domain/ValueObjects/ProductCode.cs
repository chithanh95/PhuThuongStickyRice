using PhuThuongStickyRice.Domain.Exceptions;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a product SKU/code. Always stored normalized (trimmed, upper-cased).
    /// </summary>
    public sealed class ProductCode : ValueObject
    {
        public const int MaxLength = 20;

        public string Value { get; private set; }

        private ProductCode()
        {
        }

        private ProductCode(string value)
        {
            Value = value;
        }

        public static ProductCode Of(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException("Product code must not be empty.");
            }

            var normalized = value.Trim().ToUpperInvariant();

            if (normalized.Length > MaxLength)
            {
                throw new DomainException($"Product code must not exceed {MaxLength} characters.");
            }

            return new ProductCode(normalized);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
