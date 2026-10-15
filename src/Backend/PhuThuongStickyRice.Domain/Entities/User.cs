using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.Entities
{
    public class User : Entity<Guid>, IAggregateRoot
    {
        public string UserName { get; set; }

        public string NormalizedUserName { get; set; }

        public string Email { get; set; }

        public string NormalizedEmail { get; set; }

        public bool EmailConfirmed { get; set; }

        public string PasswordHash { get; set; }

        /// <summary>
        /// TOTP secret (Base32) for admin 2FA. Null when 2FA is not set up.
        /// </summary>
        public string TotpSecret { get; set; }

        public string PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        public bool TwoFactorEnabled { get; set; }

        public string ConcurrencyStamp { get; set; }

        public string SecurityStamp { get; set; }

        public bool LockoutEnabled { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }

        public int AccessFailedCount { get; set; }

        /// <summary>
        /// Taste preferences of a registered customer (item 90), stored as
        /// JSON: {"tags":["Ít hành","Nhiều ruốc"],"note":"..."}. Prefilled
        /// into the order note at checkout; the customer can edit it there.
        /// </summary>
        public string TastePreferences { get; set; }

        /// <summary>
        /// Customer's date of birth (item 92). Editable at most once a year
        /// (see <see cref="DateOfBirthUpdatedAt"/>).
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        public DateTimeOffset? DateOfBirthUpdatedAt { get; set; }

        /// <summary>
        /// Year the birthday gift was granted (null = never). Prevents
        /// re-gifting within the same year.
        /// </summary>
        public int? BirthdayGiftYear { get; set; }

        /// <summary>The birthday gift code granted this year (if any).</summary>
        public string BirthdayGiftCode { get; set; }

        public IList<UserToken> Tokens { get; set; }

        public IList<UserClaim> Claims { get; set; }

        public IList<UserRole> UserRoles { get; set; }

        public IList<PasswordHistory> PasswordHistories { get; set; }
    }
}
