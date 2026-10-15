using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PhuThuongStickyRice.Application.Catalog.DTOs
{
    public class ToppingDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public decimal ExtraPrice { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateUpdateToppingDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ExtraPrice { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ComboItemDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }
    }

    public class ComboDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public List<ComboItemDto> Items { get; set; } = new List<ComboItemDto>();
    }

    public class CreateUpdateComboItemDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class CreateUpdateComboDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public List<CreateUpdateComboItemDto> Items { get; set; } = new List<CreateUpdateComboItemDto>();
    }

    public class QuoteRequestDto : EntityDto<Guid>
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public DateTime? EventDate { get; set; }

        public int Quantity { get; set; }

        public string Notes { get; set; }

        public string EventLocation { get; set; }

        public string QuoteType { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateQuoteRequestDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string CustomerPhone { get; set; }

        public DateTime? EventDate { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 10;

        [StringLength(1000)]
        public string Notes { get; set; }

        [StringLength(500)]
        public string EventLocation { get; set; }

        [StringLength(50)]
        public string QuoteType { get; set; }
    }

    public class UpdateQuoteRequestStatusDto
    {
        [Required]
        public string Status { get; set; }
    }

    public class GetQuoteRequestsInput : PagedAndSortedResultRequestDto
    {
        public string SearchTerm { get; set; }

        public string Status { get; set; }
    }

    public class DishCandidateDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public long Votes { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateUpdateDishCandidateDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
