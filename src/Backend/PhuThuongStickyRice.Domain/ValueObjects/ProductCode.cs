using PhuThuongStickyRice.Domain.Exceptions;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.ValueObjects
{
    /// <summary>
    /// Value object mã sản phẩm (SKU): luôn lưu đã chuẩn hóa (trim, viết
    /// hoa), tối đa <see cref="MaxLength"/> ký tự.
    /// </summary>
    public sealed class ProductCode : ValueObject
    {
        /// <summary>Độ dài tối đa của mã sản phẩm: mã do admin nhập tay nên giới hạn này giữ cho mã ngắn, dễ đọc.</summary>
        public const int MaxLength = 20;

        public string Value { get; private set; }

        private ProductCode()
        {
        }

        private ProductCode(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Tạo mã sản phẩm đã chuẩn hóa (trim + viết hoa). Ném
        /// <see cref="DomainException"/> khi rỗng hoặc vượt quá
        /// <see cref="MaxLength"/> ký tự.
        /// </summary>
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
