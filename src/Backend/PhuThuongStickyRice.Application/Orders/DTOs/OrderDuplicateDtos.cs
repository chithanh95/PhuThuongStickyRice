using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// Item 142: one group of possibly-duplicate orders — same phone,
    /// placed within the last 30 minutes, still Pending or Confirmed.
    /// </summary>
    public class DuplicateOrderGroupDto
    {
        public string Phone { get; set; }

        public string CustomerName { get; set; }

        public List<OrderDto> Orders { get; set; } = new List<OrderDto>();
    }

    /// <summary>
    /// Item 142: admin explicitly merges orders into the primary order.
    /// The merged orders are cancelled with a note; never automatic.
    /// </summary>
    public class MergeOrdersInput
    {
        [Required]
        public Guid PrimaryOrderId { get; set; }

        [Required]
        [MinLength(1)]
        public List<Guid> OrderIds { get; set; }
    }
}
