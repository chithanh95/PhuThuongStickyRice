using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        public class AddressBookDto
        {
            public Guid Id { get; set; }
            public string Phone { get; set; }
            public string Label { get; set; }
            public string Address { get; set; }
        }

        public class SaveAddressDto
        {
            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            [Required]
            [StringLength(100)]
            public string Label { get; set; }

            [Required]
            [StringLength(500)]
            public string Address { get; set; }
        }
}
