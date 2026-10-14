using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Customer wallet (item 140): prepaid balance keyed by phone, used at
    /// checkout ("Thanh toán bằng ví") and refunded on order cancel.
    /// Amounts in VND.
    /// </summary>
    public class WalletAccount : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;

        public string Phone { get; set; }

        public decimal Balance { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
