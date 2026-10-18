using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        public class CustomerPhotoDto
        {
            public Guid Id { get; set; }
            public string ImageUrl { get; set; }
            public string Caption { get; set; }
            public string Name { get; set; }
            public bool IsApproved { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class CreateCustomerPhotoDto
        {
            [StringLength(30)]
            public string Phone { get; set; }

            [StringLength(300)]
            public string Caption { get; set; }

            [StringLength(100)]
            public string Name { get; set; }
        }
}
