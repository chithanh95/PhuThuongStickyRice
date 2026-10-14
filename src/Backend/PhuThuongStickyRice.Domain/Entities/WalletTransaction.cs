using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Immutable wallet ledger entry (item 140): every top-up, manual
    /// deduct, checkout charge and cancel refund is logged here.
    /// Amounts in VND, signed (positive = credit).
    /// </summary>
    public class WalletTransaction : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;
        public const int ReasonMaxLength = 500;

        public string Phone { get; set; }

        /// <summary>Signed amount: positive = credit, negative = debit.</summary>
        public decimal Amount { get; set; }

        public string Reason { get; set; }

        /// <summary>Balance after applying this transaction.</summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>Admin user email (manual ops) or "system".</summary>
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
