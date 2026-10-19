using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Subscriptions.DTOs
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }

        public string Phone { get; set; }

        public string CustomerName { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string SizeName { get; set; }

        public string ToppingNames { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public string DeliveryAddress { get; set; }

        public DateTime StartDate { get; set; }

        public bool Active { get; set; }

        public string Notes { get; set; }

        public DateTime? LastOrderDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateSubscriptionDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        public Guid? SizeOptionId { get; set; }

        public List<Guid> ToppingIds { get; set; }
            = new List<Guid>();

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        [Required]
        [StringLength(500)]
        public string DeliveryAddress { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
