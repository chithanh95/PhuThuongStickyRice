using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Customer
{
        /// <summary>
        /// Khách hàng doanh nghiệp đặt xôi định kỳ theo tuần (vd: suất ăn
        /// sáng công ty). Trả về cho admin để quản lý lịch giao cố định.
        /// </summary>
        public class CorporateClientDto
        {
            /// <summary>Id khách hàng doanh nghiệp.</summary>
            public Guid Id { get; set; }

            /// <summary>Tên công ty/khách hàng doanh nghiệp.</summary>
            public string CompanyName { get; set; }

            /// <summary>Tên người liên hệ đặt hàng.</summary>
            public string ContactName { get; set; }

            /// <summary>Số điện thoại liên hệ đặt hàng.</summary>
            public string Phone { get; set; }

            /// <summary>Địa chỉ giao hàng của công ty.</summary>
            public string Address { get; set; }

            /// <summary>Ngày giao cố định hàng tuần: 0 = Chủ nhật … 6 = Thứ bảy.</summary>
            public int Weekday { get; set; }

            /// <summary>Tên ngày giao cố định (vd: "Thứ Hai") — hiển thị cho admin.</summary>
            public string WeekdayName { get; set; }

            /// <summary>Chuỗi JSON các món đặt cố định mỗi tuần.</summary>
            public string ItemsJson { get; set; }

            /// <summary>False = tạm ngưng lịch giao định kỳ cho khách này.</summary>
            public bool IsActive { get; set; }
        }

        /// <summary>Dữ liệu tạo mới hoặc cập nhật khách hàng doanh nghiệp.</summary>
        public class CreateUpdateCorporateClientDto
        {
            /// <summary>Tên công ty (bắt buộc, tối đa 200 ký tự).</summary>
            [Required]
            [StringLength(200)]
            public string CompanyName { get; set; }

            /// <summary>Tên người liên hệ đặt hàng (tối đa 100 ký tự).</summary>
            [StringLength(100)]
            public string ContactName { get; set; }

            /// <summary>Số điện thoại liên hệ (bắt buộc, tối đa 30 ký tự).</summary>
            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            /// <summary>Địa chỉ giao hàng của công ty (tối đa 500 ký tự).</summary>
            [StringLength(500)]
            public string Address { get; set; }

            /// <summary>0 = Sunday … 6 = Saturday.</summary>
            [Range(0, 6)]
            public int Weekday { get; set; }

            /// <summary>Chuỗi JSON các món đặt cố định mỗi tuần.</summary>
            public string ItemsJson { get; set; }

            /// <summary>True = đang giao hàng định kỳ (mặc định true).</summary>
            public bool IsActive { get; set; } = true;
        }
}
