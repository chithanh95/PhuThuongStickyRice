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

        public DateTime? AvailableTo { get; set; }
    }
}
