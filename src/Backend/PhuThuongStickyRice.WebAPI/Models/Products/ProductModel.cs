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

        /// <summary>
        /// Item 164: daily sale time windows as JSON, e.g.
        /// [{"from":"06:00","to":"10:00"}]. Null = sold all day.
        /// </summary>
        public string AvailableHoursJson { get; set; }

        /// <summary>Item 165: max portions sold per day. Null = unlimited.</summary>
        public int? DailyLimit { get; set; }

        /// <summary>
        /// Item 166: photo gallery as a JSON string array of image URLs
        /// (first = primary).
        /// </summary>
        public string ImageUrlsJson { get; set; }

        public DateTime? AvailableTo { get; set; }
    }
}
