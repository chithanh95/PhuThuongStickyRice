using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Loyalty
{
        public class OrderRewardRuleDto
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public decimal MinAmount { get; set; }

            /// <summary>"FreeTopping" or "DiscountCode".</summary>
            public string RewardType { get; set; }

            public string ToppingName { get; set; }

            public decimal? DiscountPercent { get; set; }

            public bool IsActive { get; set; }
        }

        public class CreateOrderRewardRuleDto
        {
            [Required]
            [StringLength(200)]
            public string Name { get; set; }

            [Range(0, double.MaxValue)]
            public decimal MinAmount { get; set; }

            [Required]
            public string RewardType { get; set; }

            [StringLength(200)]
            public string ToppingName { get; set; }

            [Range(0.01, 100)]
            public decimal? DiscountPercent { get; set; }

            public bool IsActive { get; set; } = true;
        }
}
