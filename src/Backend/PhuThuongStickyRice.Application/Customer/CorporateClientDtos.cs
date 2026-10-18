using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        public class CorporateClientDto
        {
            public Guid Id { get; set; }
            public string CompanyName { get; set; }
            public string ContactName { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            public int Weekday { get; set; }
            public string WeekdayName { get; set; }
            public string ItemsJson { get; set; }
            public bool IsActive { get; set; }
        }

        public class CreateUpdateCorporateClientDto
        {
            [Required]
            [StringLength(200)]
            public string CompanyName { get; set; }

            [StringLength(100)]
            public string ContactName { get; set; }

            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            [StringLength(500)]
            public string Address { get; set; }

            /// <summary>0 = Sunday … 6 = Saturday.</summary>
            [Range(0, 6)]
            public int Weekday { get; set; }

            public string ItemsJson { get; set; }

            public bool IsActive { get; set; } = true;
        }
}
