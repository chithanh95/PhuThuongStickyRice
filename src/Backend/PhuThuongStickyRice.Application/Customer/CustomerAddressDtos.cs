using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
    public class CustomerAddressDto
    {
        public Guid Id { get; set; }

        public string Label { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string AddressDetail { get; set; }

        public string Note { get; set; }

        public bool IsDefault { get; set; }
    }

    public class CreateCustomerAddressDto
    {
        [MaxLength(100)]
        public string Label { get; set; }

        [Required]
        [MaxLength(200)]
        public string ReceiverName { get; set; }

        [Required]
        [MaxLength(30)]
        public string ReceiverPhone { get; set; }

        [Required]
        [MaxLength(500)]
        public string AddressDetail { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        public bool IsDefault { get; set; }
    }

    public class UpdateCustomerAddressDto : CreateCustomerAddressDto
    {
    }
}
