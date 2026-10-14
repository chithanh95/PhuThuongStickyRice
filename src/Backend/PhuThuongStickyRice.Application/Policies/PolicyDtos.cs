using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Policies.DTOs
{
    public class PolicyPageDto
    {
        public Guid Id { get; set; }

        public string Slug { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public int SortOrder { get; set; }
    }

    /// <summary>Public: slug + title only, for footer links.</summary>
    public class PolicyPageSummaryDto
    {
        public string Slug { get; set; }

        public string Title { get; set; }
    }

    /// <summary>Admin: edit title/body/order. The slug is immutable.</summary>
    public class UpdatePolicyPageDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Body { get; set; }

        public int SortOrder { get; set; }
    }
}
