using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Staff
{
        /// <summary>
        /// 1 ca làm việc của nhân viên trong ngày (giờ bắt đầu/kết thúc + ghi chú).
        /// </summary>
        public class WorkShiftDto
        {
            /// <summary>Id ca làm việc.</summary>
            public Guid Id { get; set; }

            /// <summary>Tên nhân viên làm ca.</summary>
            public string StaffName { get; set; }

            /// <summary>Ngày làm ca (phần ngày, bỏ giờ).</summary>
            public DateTime Date { get; set; }

            /// <summary>Giờ bắt đầu ca (vd 06:00).</summary>
            public TimeSpan StartTime { get; set; }

            /// <summary>Giờ kết thúc ca (vd 10:30).</summary>
            public TimeSpan EndTime { get; set; }

            /// <summary>Ghi chú thêm về ca (tối đa 500 ký tự).</summary>
            public string Note { get; set; }
        }

        /// <summary>
        /// Dữ liệu tạo/sửa ca làm việc: tên nhân viên bắt buộc (tối đa 100 ký tự).
        /// </summary>
        public class CreateUpdateWorkShiftDto
        {
            /// <summary>Tên nhân viên (bắt buộc, tối đa 100 ký tự).</summary>
            [Required]
            [StringLength(100)]
            public string StaffName { get; set; }

            /// <summary>Ngày làm ca.</summary>
            public DateTime Date { get; set; }

            /// <summary>Giờ bắt đầu ca.</summary>
            public TimeSpan StartTime { get; set; }

            /// <summary>Giờ kết thúc ca.</summary>
            public TimeSpan EndTime { get; set; }

            /// <summary>Ghi chú thêm (tối đa 500 ký tự).</summary>
            [StringLength(500)]
            public string Note { get; set; }
        }
}
