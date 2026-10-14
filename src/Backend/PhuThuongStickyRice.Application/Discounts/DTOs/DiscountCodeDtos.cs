using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Discounts.DTOs
{
    public class DiscountCodeDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        /// <summary>0-100. Null when fixed-amount.</summary>
        [Range(0, 100)]
        public decimal? DiscountPercent { get; set; }

        /// <summary>Fixed VND amount. Null when percent-based.</summary>
        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinOrderAmount { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxUses { get; set; }

        public int UsedCount { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateDiscountCodeDto
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Range(0, 100)]
        public decimal? DiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinOrderAmount { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxUses { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ValidateDiscountInput
    {
        [Required]
        public string Code { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }
    }

    public class ValidateDiscountResultDto
    {
        public bool Valid { get; set; }

        public string Message { get; set; }

        /// <summary>Discount in VND for the given subtotal (0 when invalid).</summary>
        public decimal DiscountAmount { get; set; }
    }
}
