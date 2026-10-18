using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Discounts
{
        public class VoucherCampaignAudienceDto
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public int RecipientCount { get; set; }
        }

        public class CreateVoucherCampaignInput
        {
            /// <summary>"inactive30", "tier", or "all".</summary>
            [Required]
            public string Audience { get; set; }

            /// <summary>Only for audience "tier": "Bạc" or "Vàng".</summary>
            public string Tier { get; set; }

            /// <summary>Percent discount, or fixed amount when DiscountAmount is set.</summary>
            [Range(1, 100)]
            public decimal DiscountPercent { get; set; }

            /// <summary>Fixed VND amount (used instead of percent when > 0).</summary>
            [Range(0, 10000000)]
            public decimal DiscountAmount { get; set; }

            /// <summary>Days until expiry. Default 30.</summary>
            [Range(1, 365)]
            public int ValidDays { get; set; } = 30;

            [StringLength(50)]
            public string Prefix { get; set; } = "VC";
        }

        public class GeneratedVoucherDto
        {
            public string Phone { get; set; }
            public string Code { get; set; }
        }
}
