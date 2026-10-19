using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        /// <summary>
        /// Một địa chỉ đã lưu trong sổ địa chỉ của khách: nhãn tự đặt (Nhà, Cơ quan...)
        /// và địa chỉ chi tiết; khách chọn nhanh ở checkout thay vì gõ lại.
        /// </summary>
        public class AddressBookDto
        {
            public Guid Id { get; set; }
            public string Phone { get; set; }
            public string Label { get; set; }
            public string Address { get; set; }
        }

        /// <summary>
        /// Dữ liệu lưu địa chỉ mới vào sổ: SĐT, nhãn và địa chỉ chi tiết bắt buộc
        /// (địa chỉ tối đa 500 ký tự).
        /// </summary>
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
