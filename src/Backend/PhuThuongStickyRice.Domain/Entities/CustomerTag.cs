using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Tag gắn cho khách hàng theo SĐT (item 149), VD: VIP, Nợ, Khó tính,
    /// Khách sỉ. Khách được tổng hợp theo SĐT (không bắt buộc có tài khoản
    /// thành viên) nên tag key theo <see cref="CustomerPhone"/>.
    /// </summary>
    public class CustomerTag : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;
        public const int TagNameMaxLength = 50;
        public const int ColorMaxLength = 20;
        public const int CreatedByMaxLength = 100;

        public string CustomerPhone { get; set; }

        public string TagName { get; set; }

        /// <summary>Hex color for the badge, e.g. "#e74c3c".</summary>
        public string Color { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
