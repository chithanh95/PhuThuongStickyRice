using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        public class CustomerNoteDto
        {
            public Guid Id { get; set; }

            public string Phone { get; set; }

            public string Note { get; set; }

            public string CreatedBy { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        public class CreateCustomerNoteDto
        {
            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            [Required]
            [StringLength(2000)]
            public string Note { get; set; }
        }
}
