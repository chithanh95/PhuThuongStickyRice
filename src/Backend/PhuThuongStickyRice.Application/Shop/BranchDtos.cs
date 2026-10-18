using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop
{
        public class BranchDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public bool IsActive { get; set; }
        }

        public class CreateUpdateBranchDto
        {
            [Required]
            [StringLength(200)]
            public string Name { get; set; }

            [StringLength(500)]
            public string Address { get; set; }

            [StringLength(30)]
            public string Phone { get; set; }

            public bool IsActive { get; set; } = true;
        }
}
