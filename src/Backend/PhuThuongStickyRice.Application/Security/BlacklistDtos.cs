using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Security
{
        public class BlacklistedPhoneDto
        {
            public Guid Id { get; set; }

            public string Phone { get; set; }

            public string Reason { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        public class CreateBlacklistedPhoneDto
        {
            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            [StringLength(500)]
            public string Reason { get; set; }
        }
}
