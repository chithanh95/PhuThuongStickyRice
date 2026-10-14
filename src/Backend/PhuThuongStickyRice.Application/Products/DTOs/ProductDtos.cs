using System;
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
    }

    public class GetProductsInput : PagedAndSortedResultRequestDto
    {
        public string SearchTerm { get; set; }

        public string Status { get; set; }
    }
}
