using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// A customer referral: an existing customer (referrer) invites a friend
    /// (referred). When the friend places their first order, the referrer
    /// gets a reward (default 10.000 VND credit/discount).
    /// </summary>
    public class Referral : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 15;

        public string ReferrerPhone { get; set; }

        public string ReferredPhone { get; set; }

        public int RewardAmount { get; set; } = 10000;

        public bool IsRewarded { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
