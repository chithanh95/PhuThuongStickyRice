using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        /// <summary>
        /// Ghi chú nội bộ của staff về một khách hàng (tra theo SĐT): dùng để lưu
        /// nhớ xử lý đặc biệt (dị ứng, địa chỉ khó tìm, thái độ...) khi nhân viên
        /// nhận đơn hoặc giao hàng.
        /// </summary>
        public class CustomerNoteDto
        {
            public Guid Id { get; set; }

            public string Phone { get; set; }

            public string Note { get; set; }

            public string CreatedBy { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Dữ liệu tạo ghi chú khách hàng mới: SĐT (để tra khách) và nội dung
        /// ghi chú bắt buộc; độ dài nội dung tối đa 2000 ký tự.
        /// </summary>
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
