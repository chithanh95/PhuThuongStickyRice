using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Support.DTOs
{
    public class SupportTicketDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public string OrderNumber { get; set; }

        public string Message { get; set; }

        public string Status { get; set; }

        public string ReplyNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RepliedAt { get; set; }
    }

    public class CreateSupportTicketDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        [StringLength(30)]
        public string OrderNumber { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; }
    }

    public class ReplySupportTicketDto
    {
        [Required]
        [StringLength(2000)]
        public string ReplyNote { get; set; }

        /// <summary>"InProgress" or "Done".</summary>
        [Required]
        public string Status { get; set; }
    }
}
