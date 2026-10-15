using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Web.Components.Products.Models
{
    public class ProductModel
    {
        public Guid Id { get; set; }

        [Required]
        [MinLength(3)]
        public string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string Currency { get; set; } = "VND";

        /// <summary>Cost price (giá vốn) in VND, for the profit report.</summary>
        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        /// <summary>Item 81: selling unit — "suất" (per portion) or "kg" (per kilogram).</summary>
        public string Unit { get; set; } = "suất";

        public string Status { get; set; }

        /// <summary>Comma-separated dietary tags: "chay", "man", "cay".</summary>
        public string Tags { get; set; }

        public string Category { get; set; }

        public string AllergenInfo { get; set; }

        public bool IsNew { get; set; }

        public bool IsSignature { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        /// <summary>Item 109: optional branch this product belongs to; null = all branches.</summary>
        public Guid? BranchId { get; set; }

        public string BranchName { get; set; }
    }
}
