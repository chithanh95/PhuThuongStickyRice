using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Feature flag (cờ tính năng). Admin toggles these at
    /// <c>/admin/tinh-nang</c>; the UI and app services gate optional
    /// features behind them. Seeded defaults keep everything ON so
    /// nothing changes for existing behavior until the admin flips a flag.
    /// </summary>
    public class FeatureFlag : Entity<Guid>, IAggregateRoot
    {
        public const string LuckySpin = "lucky-spin";
        public const string GiftWrap = "gift-wrap";
        public const string Referrals = "referrals";
        public const string DishVoting = "dish-voting";
        public const string PromoPopup = "promo-popup";

        public const int KeyMaxLength = 100;
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 500;

        /// <summary>Stable key, e.g. "lucky-spin".</summary>
        public string Key { get; set; }

        /// <summary>Vietnamese display name.</summary>
        public string Name { get; set; }

        public bool IsEnabled { get; set; }

        public string Description { get; set; }
    }
}
