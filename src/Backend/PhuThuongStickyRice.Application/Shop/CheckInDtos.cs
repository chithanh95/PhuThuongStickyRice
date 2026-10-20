using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop
{
        /// <summary>
        /// Kết quả điểm danh / trạng thái điểm danh của 1 SĐT: đã điểm danh
        /// hôm nay chưa, số điểm được tặng, streak và tổng điểm loyalty.
        /// </summary>
        public class CheckInResultDto
        {
            /// <summary>True khi SĐT đã điểm danh hôm nay (không cộng điểm mới).</summary>
            public bool AlreadyCheckedIn { get; set; }

            /// <summary>Số điểm được tặng trong lần điểm danh này (0 nếu đã điểm danh rồi).</summary>
            public int PointsAwarded { get; set; }

            /// <summary>Số ngày điểm danh liên tiếp tính đến hôm nay.</summary>
            public int StreakDays { get; set; }

            /// <summary>Tổng điểm loyalty hiện tại của SĐT.</summary>
            public int LoyaltyPoints { get; set; }
        }
    /// <summary>Body của API điểm danh (public, không cần đăng nhập).</summary>
    public class CheckInRequestModel
    {
        /// <summary>SĐT khách điểm danh (server tự chuẩn hóa đầu số Việt Nam).</summary>
        public string Phone { get; set; }
    }
}
