using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
        public class GroupOrderLineDto
        {
            public Guid ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public string Note { get; set; }
            public string AddedBy { get; set; }
        }

        public class GroupOrderDto
        {
            public Guid Id { get; set; }
            public string Code { get; set; }
            public string CreatorPhone { get; set; }
            public string GroupName { get; set; }
            public DateTime? CutoffAt { get; set; }
            public string Status { get; set; }
            public List<GroupOrderLineDto> Lines { get; set; } = new List<GroupOrderLineDto>();
            public DateTime CreatedAt { get; set; }
            public DateTime? ClosedAt { get; set; }
            public string ResultOrderNumber { get; set; }
        }

        public class CreateGroupOrderDto
        {
            [Required]
            [StringLength(30)]
            public string CreatorPhone { get; set; }

            [StringLength(100)]
            public string GroupName { get; set; }

            public DateTime? CutoffAt { get; set; }
        }

        public class AddGroupOrderLineDto
        {
            [Required]
            public Guid ProductId { get; set; }

            [Range(1, 100)]
            public int Quantity { get; set; }

            [StringLength(200)]
            public string Note { get; set; }

            [StringLength(100)]
            public string AddedBy { get; set; }
        }

        public class CloseGroupOrderDto
        {
            [Required]
            [StringLength(200)]
            public string CustomerName { get; set; }

            [Required]
            [StringLength(30)]
            public string CustomerPhone { get; set; }

            [Required]
            [StringLength(500)]
            public string CustomerAddress { get; set; }

            [StringLength(1000)]
            public string Notes { get; set; }
        }
}
