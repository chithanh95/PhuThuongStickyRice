using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Staff
{
        /// <summary>
        /// Bản chốt ca (đối soát tiền mặt COD cuối ca, Item 129): so tiền mặt
        /// hệ thống kỳ vọng với tiền thu thực tế để phát hiện chênh lệch.
        /// </summary>
        public class ShiftCloseDto
        {
            /// <summary>Id bản chốt ca.</summary>
            public Guid Id { get; set; }

            /// <summary>Ngày chốt ca (1 ca = 1 ngày).</summary>
            public DateTime Date { get; set; }

            /// <summary>Tiền mặt COD hệ thống tính ra phải thu trong ngày.</summary>
            public decimal ExpectedCod { get; set; }

            /// <summary>Tiền mặt thu thực tế (thu ngân nhập tay khi chốt ca).</summary>
            public decimal ActualCod { get; set; }

            /// <summary>Chênh lệch = ActualCod - ExpectedCod (âm = thiếu tiền).</summary>
            public decimal Discrepancy { get; set; }

            /// <summary>Ghi chú của thu ngân khi chốt ca.</summary>
            public string Note { get; set; }

            /// <summary>Tên người chốt ca.</summary>
            public string CreatedBy { get; set; }

            /// <summary>Thời điểm chốt ca.</summary>
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Dữ liệu chốt ca: ngày + tiền mặt thu thực tế + ghi chú. Đã chốt
        /// ngày nào thì cập nhật đè lên bản chốt cũ (không tạo bản trùng).
        /// </summary>
        public class CloseShiftInput
        {
            /// <summary>Ngày cần chốt ca.</summary>
            public DateTime Date { get; set; }

            /// <summary>Tiền mặt COD thu thực tế (VNĐ, không âm).</summary>
            [Range(0, 1000000000)]
            public decimal ActualCod { get; set; }

            /// <summary>Ghi chú khi chốt ca (tối đa 500 ký tự).</summary>
            [StringLength(500)]
            public string Note { get; set; }
        }
}
