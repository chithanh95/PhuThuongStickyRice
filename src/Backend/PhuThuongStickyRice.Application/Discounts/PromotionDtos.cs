using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Discounts
{
        public class PromotionDto
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public decimal DiscountPercent { get; set; }

            /// <summary>Local start time "HH:mm".</summary>
            public string StartTime { get; set; }

            /// <summary>Local end time "HH:mm".</summary>
            public string EndTime { get; set; }

            /// <summary>Bit flags of PromotionDay (1=Mon .. 64=Sun, 127=all).</summary>
            public int DaysOfWeekFlags { get; set; }

            public bool IsActive { get; set; }
        }

        public class CreatePromotionDto
        {
            [Required]
            [StringLength(200)]
            public string Name { get; set; }

            [Range(0.01, 100)]
            public decimal DiscountPercent { get; set; }

            /// <summary>"HH:mm".</summary>
            [Required]
            public string StartTime { get; set; }

            /// <summary>"HH:mm".</summary>
            [Required]
            public string EndTime { get; set; }

            [Range(1, 127)]
            public int DaysOfWeekFlags { get; set; } = 127;

            public bool IsActive { get; set; } = true;
        }
}
