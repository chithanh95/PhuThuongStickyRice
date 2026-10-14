using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Login attempt audit. A successful login from an IP never seen before
    /// for that user raises a "new IP" warning on the dashboard.
    /// </summary>
    public class LoginAudit : Entity<Guid>, IAggregateRoot
    {
        public const int UserNameMaxLength = 256;
        public const int IpMaxLength = 64;
        public const int ReasonMaxLength = 500;

        public string UserName { get; set; }

        public DateTime At { get; set; } = DateTime.UtcNow;

        public string IpAddress { get; set; }

        public bool Success { get; set; }

        public string FailureReason { get; set; }
    }
}
