using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Faq.DTOs
{
    public class FaqItemDto
    {
        public Guid Id { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateFaqItemDto
    {
        [Required]
        [StringLength(500)]
        public string Question { get; set; }

        [Required]
        [StringLength(4000)]
        public string Answer { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
