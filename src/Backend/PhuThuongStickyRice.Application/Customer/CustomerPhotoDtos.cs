using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        /// <summary>
        /// Ảnh do khách gửi về (check-in/feedback món ăn): hiển thị ở trang
        /// cộng đồng khi đã được admin duyệt (<see cref="IsApproved"/>).
        /// </summary>
        public class CustomerPhotoDto
        {
            public Guid Id { get; set; }
            public string ImageUrl { get; set; }
            public string Caption { get; set; }
            public string Name { get; set; }

            /// <summary>Đã được admin duyệt hiển thị công khai hay chưa.</summary>
            public bool IsApproved { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Dữ liệu khách gửi ảnh: SĐT, chú thích (tối đa 300 ký tự) và tên hiển thị
        /// đều tùy chọn; URL ảnh do server xử lý upload, client không gửi.
        /// </summary>
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
