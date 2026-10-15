using System;

namespace PhuThuongStickyRice.WebAPI.Models.Products
{
    public class ProductModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        public string Status { get; set; }

        public string Tags { get; set; }

        public string Category { get; set; }

        public string AllergenInfo { get; set; }

        /// <summary>Item 84: cost price for the profit report.</summary>
        public decimal CostPrice { get; set; }

        public bool IsNew { get; set; }

        public bool IsSignature { get; set; }

        public DateTime? AvailableFrom { get; set; }

        /// <summary>Item 109: branch the product belongs to (null = all branches).</summary>
        public Guid? BranchId { get; set; }

        /// <summary>Item 81: selling unit — "suất" (per portion) or "kg" (per kilogram).</summary>
        public string Unit { get; set; }

        public DateTime? AvailableTo { get; set; }

        /// <summary>
        /// Item 130: when true, the product is auto-posted to the Facebook
        /// Page feed after saving (create or update). Ignored silently when
        /// Facebook is not configured.
        /// </summary>
        public bool PostToFacebook { get; set; }
    }
}
