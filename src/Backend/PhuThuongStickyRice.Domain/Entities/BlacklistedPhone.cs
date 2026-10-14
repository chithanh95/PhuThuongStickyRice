using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Phone numbers blocked from placing orders (spam, fake orders, ...).
    /// Checkout rejects these phones with a fixed message.
    /// </summary>
    public class BlacklistedPhone : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;
        public const int ReasonMaxLength = 500;

        public string Phone { get; set; }

        /// <summary>Normalized (digits only) for reliable matching.</summary>
        public string NormalizedPhone { get; set; }

        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
