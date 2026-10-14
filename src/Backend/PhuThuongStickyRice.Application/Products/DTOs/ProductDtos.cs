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

        /// <summary>
        /// Only applied on update: "Active", "Inactive" or "Discontinued".
        /// </summary>
        public string Status { get; set; }

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
    }

    public class GetProductsInput : PagedAndSortedResultRequestDto
    {
        public string SearchTerm { get; set; }

        public string Status { get; set; }
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
}
