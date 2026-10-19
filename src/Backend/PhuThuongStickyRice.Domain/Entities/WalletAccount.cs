using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Ví trả trước của khách (item 140): số dư nạp trước, gắn theo SĐT.
    /// Dùng khi checkout chọn "Thanh toán bằng ví"; hủy đơn thì tiền
    /// được hoàn về ví. Mọi số dư tính bằng VND.
    /// </summary>
    public class WalletAccount : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Độ dài tối đa cột SĐT trong DB.</summary>
        public const int PhoneMaxLength = 30;

        /// <summary>SĐT chủ ví (duy nhất) — dùng để tra số dư khi checkout.</summary>
        public string Phone { get; set; }

        /// <summary>Số dư hiện tại (VND). Số dư hợp lệ không bao giờ âm.</summary>
        public decimal Balance { get; set; }

        /// <summary>Thời điểm tạo ví (UTC).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Thời điểm ví được cập nhật lần cuối (UTC).</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
