using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Địa chỉ giao hàng đã lưu của khách (sổ địa chỉ): nhãn (Nhà/Riêng...), người nhận,
    /// SĐT, địa chỉ chi tiết, ghi chú giao hàng và cờ địa chỉ mặc định dùng ở checkout.
    /// </summary>
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

    /// <summary>
    /// Dữ liệu thêm địa chỉ mới vào sổ địa chỉ: người nhận, SĐT, địa chỉ chi tiết
    /// bắt buộc; nhãn và ghi chú tùy chọn.
    /// </summary>
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

    /// <summary>
    /// Dữ liệu sửa địa chỉ trong sổ địa chỉ (cùng các trường với lúc tạo mới).
    /// </summary>
    public class UpdateCustomerAddressDto : CreateCustomerAddressDto
    {
    }
}
