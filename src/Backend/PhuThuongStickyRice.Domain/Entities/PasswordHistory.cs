using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Lịch sử mật khẩu của user, dùng để chặn user đặt lại mật khẩu cũ
    /// (chính sách "không được dùng lại N mật khẩu gần nhất").
    /// Chỉ lưu hash, không bao giờ lưu mật khẩu dạng text.
    /// </summary>
    public class PasswordHistory : Entity<Guid>
    {
        public Guid UserId { get; set; }

        public string PasswordHash { get; set; }

        public virtual User User { get; set; }
    }
}