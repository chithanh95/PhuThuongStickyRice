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
        public const int IdempotencyKeyMaxLength = 120;

        public string Phone { get; set; }

        /// <summary>Signed amount: positive = credit, negative = debit.</summary>
        public decimal Amount { get; set; }

        public string Reason { get; set; }

        /// <summary>
        /// Khóa chống hoàn tiền trùng — CHỈ <c>WalletAppService.RefundAsync</c>
        /// đặt, dạng "refund|{phone}|{sha256(reason)}". Mọi dòng khác (nạp,
        /// trừ tay, charge khi checkout) để NULL. Unique index bỏ qua NULL
        /// (SQL Server/SQLite/Postgres) nên các dòng không có khóa không bao
        /// giờ va chạm — khác với unique (Phone, Reason) sẽ làm vỡ các lần
        /// charge hợp lệ lặp lại cùng lý do "Thanh toán đơn {mã}".
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>Balance after applying this transaction.</summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>Admin user email (manual ops) or "system".</summary>
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
