using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
        public class ShipperDto
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public string Phone { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        public class CreateShipperDto
        {
            [Required]
            [StringLength(100)]
            public string Name { get; set; }

            [StringLength(30)]
            public string Phone { get; set; }
        }

        public class UpdateShipperDto : CreateShipperDto
        {
            public bool IsActive { get; set; } = true;
        }
}
