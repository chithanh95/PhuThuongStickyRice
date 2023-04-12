using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    public class PasswordHistory : Entity<Guid>
    {
        public Guid UserId { get; set; }

        public string PasswordHash { get; set; }

        public virtual User User { get; set; }
    }
}