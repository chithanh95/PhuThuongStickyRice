using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Sổ cái ví (item 140) — bản ghi bất biến: mỗi lần nạp tiền, trừ tay,
    /// trừ khi checkout hay hoàn khi hủy đơn đều ghi 1 dòng ở đây.
    /// Số tiền VND có dấu (dương = cộng tiền, âm = trừ tiền).
    /// </summary>
    public class WalletTransaction : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Độ dài tối đa cột SĐT trong DB.</summary>
        public const int PhoneMaxLength = 30;
        /// <summary>Độ dài tối đa cột lý do trong DB.</summary>
        public const int ReasonMaxLength = 500;
        /// <summary>Độ dài tối đa cột khóa chống trùng trong DB.</summary>
        public const int IdempotencyKeyMaxLength = 120;

        /// <summary>SĐT chủ ví — dùng để tra lịch sử giao dịch của một khách.</summary>
        public string Phone { get; set; }

        /// <summary>Số tiền có dấu: dương = nạp/hoàn tiền, âm = trừ tiền.</summary>
        public decimal Amount { get; set; }

        /// <summary>Lý do ghi tay của admin hoặc hệ thống (vd "Thanh toán đơn {mã}").</summary>
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

        /// <summary>Số dư ví SAU khi áp giao dịch này (để kiểm toán).</summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>Email admin thực hiện thao tác tay, hoặc "system" cho tự động.</summary>
        public string CreatedBy { get; set; }

        /// <summary>Thời điểm ghi giao dịch (UTC).</summary>
        public DateTime CreatedAt { get; set; }
    }
}
