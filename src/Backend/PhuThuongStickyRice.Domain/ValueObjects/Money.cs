using PhuThuongStickyRice.Domain.Exceptions;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.ValueObjects
{
    /// <summary>
    /// Value object tiền tệ: bất biến, so sánh theo giá trị (số tiền +
    /// tiền tệ). Mọi số tiền trên đơn (đơn giá, phí, giảm giá) đều dùng
    /// kiểu này thay vì decimal trần để không lẫn tiền tệ.
    /// </summary>
    public class Money : ValueObject
    {
        /// <summary>Số tiền (không âm).</summary>
        public decimal Amount { get; private set; }

        /// <summary>Mã tiền tệ ISO 4217 (ví dụ VND), luôn viết hoa.</summary>
        public string Currency { get; private set; }

        private Money()
        {
        }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        /// <summary>
        /// Tạo Money đã validate: số tiền không âm, tiền tệ là mã ISO 4217
        /// 3 ký tự (tự chuẩn hóa trim + viết hoa).
        /// </summary>
        /// <exception cref="DomainException">Ném khi số tiền âm hoặc tiền tệ sai định dạng.</exception>
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

        /// <summary>Tiền tệ 0đ — dùng khi khởi tạo các khoản phí/giảm giá chưa có giá trị.</summary>
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
