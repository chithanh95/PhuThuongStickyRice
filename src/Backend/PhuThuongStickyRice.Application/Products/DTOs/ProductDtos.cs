using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PhuThuongStickyRice.Application.Products.DTOs
{
    public class ProductDto : EntityDto<Guid>
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        /// <summary>Cost price (giá vốn) in VND, for the profit report.</summary>
        public decimal CostPrice { get; set; }

        /// <summary>Selling unit: "suất" (per portion) or "kg" (per kilogram).</summary>
        public string Unit { get; set; }

        /// <summary>Item 146: manual display order (drag &amp; drop on /products).</summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Item 164: daily sale time windows as JSON, e.g.
        /// [{"from":"06:00","to":"10:00"}]. Null = sold all day.
        /// </summary>
        public string AvailableHoursJson { get; set; }

        /// <summary>Item 165: max portions sold per day. Null = unlimited.</summary>
        public int? DailyLimit { get; set; }

        /// <summary>
        /// Item 166: photo gallery as a JSON string array of image URLs
        /// (first = primary). Null = placeholder SVG.
        /// </summary>
        public string ImageUrlsJson { get; set; }

        /// <summary>
        /// Item 165: portions still sellable today. Null = no daily limit.
        /// Filled on the public shop list only.
        /// </summary>
        public int? RemainingToday { get; set; }

        public string Status { get; set; }

        /// <summary>Comma-separated dietary tags: "chay", "man", "cay".</summary>
        public string Tags { get; set; }

        public string Category { get; set; }

        public string AllergenInfo { get; set; }

        public bool IsNew { get; set; }

        public bool IsSignature { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        public List<ProductSizeOptionDto> Sizes { get; set; } = new List<ProductSizeOptionDto>();

        /// <summary>Branch the product belongs to (null = all branches).</summary>
        public Guid? BranchId { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
    }

    public class CreateUpdateProductDto
    {
        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string Currency { get; set; } = "VND";

        /// <summary>Cost price (giá vốn) in VND, for the profit report.</summary>
        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        /// <summary>Selling unit: "suất" (per portion) or "kg" (per kilogram).</summary>
        [StringLength(20)]
        public string Unit { get; set; }

        /// <summary>
        /// Only applied on update: "Active", "Inactive" or "Discontinued".
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Item 164: daily sale time windows as JSON, e.g.
        /// [{"from":"06:00","to":"10:00"}]. Null/empty = sold all day.
        /// </summary>
        [StringLength(1000)]
        public string AvailableHoursJson { get; set; }

        /// <summary>Item 165: max portions sold per day. Null = unlimited.</summary>
        [Range(0, int.MaxValue)]
        public int? DailyLimit { get; set; }

        /// <summary>
        /// Item 166: photo gallery as a JSON string array of image URLs.
        /// </summary>
        [StringLength(4000)]
        public string ImageUrlsJson { get; set; }

        /// <summary>Comma-separated dietary tags: "chay", "man", "cay".</summary>
        [StringLength(200)]
        public string Tags { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        [StringLength(500)]
        public string AllergenInfo { get; set; }

        public bool IsNew { get; set; }

        public bool IsSignature { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        /// <summary>Branch the product belongs to (null = all branches).</summary>
        public Guid? BranchId { get; set; }
    }

    public class GetProductsInput : PagedAndSortedResultRequestDto
    {
        public string SearchTerm { get; set; }

        public string Status { get; set; }

        /// <summary>Optional branch filter (item 109): products of this
        /// branch plus branch-less (global) products.</summary>
        public Guid? BranchId { get; set; }

        /// <summary>
        /// Item 85: when true and Status=Active, Inactive (sold-out)
        /// products are included so the shop can show a "Tạm hết" badge.
        /// Discontinued products are never included.
        /// </summary>
        public bool IncludeInactive { get; set; }
    }

    public class ProductSizeOptionDto : EntityDto<Guid>
    {
        public Guid ProductId { get; set; }

        public string Name { get; set; }

        /// <summary>Absolute price of this size, in VND.</summary>
        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateUpdateProductSizeOptionDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Item 165: per-product daily-limit status for the public shop
    /// ("Còn N suất"). Only products with a DailyLimit are included.
    /// </summary>
    public class ProductDailyRemainingDto
    {
        public Guid ProductId { get; set; }

        public int DailyLimit { get; set; }

        public int SoldToday { get; set; }

        public int RemainingToday { get; set; }
    }

    /// <summary>
    /// Item 148: one row of the product change history ("ai sửa gì, khi nào").
    /// </summary>
    public class ProductChangeLogDto : EntityDto<Guid>
    {
        public Guid ProductId { get; set; }

        public string ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; }

        /// <summary>Vietnamese field label, e.g. "Giá bán".</summary>
        public string FieldName { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }
    }

    /// <summary>
    /// Wave 6b (item 26): per-product rating aggregate for the public
    /// <c>GET api/products/ratings</c> endpoint. Only non-hidden reviews
    /// count; products without reviews are omitted.
    /// </summary>
    public class ProductRatingDto
    {
        public Guid ProductId { get; set; }

        public double AverageRating { get; set; }

        public int ReviewCount { get; set; }
    }
}
