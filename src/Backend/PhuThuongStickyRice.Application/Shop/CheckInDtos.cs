using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Kết quả điểm danh hàng ngày: trạng thái đã/chưa điểm danh hôm nay,
    /// điểm được cộng lần này, chuỗi ngày liên tiếp và tổng điểm loyalty
    /// hiện tại của SĐT.
    /// </summary>
    public class CheckInResultDto
    {
        /// <summary>True nếu SĐT đã điểm danh hôm nay (không cộng thêm điểm).</summary>
        public bool AlreadyCheckedIn { get; set; }

        /// <summary>Điểm cộng từ lần điểm danh này (0 nếu đã điểm danh trước đó).</summary>
        public int PointsAwarded { get; set; }

        /// <summary>Số ngày điểm danh liên tiếp, tính cả hôm nay.</summary>
        public int StreakDays { get; set; }

        /// <summary>Tổng điểm loyalty hiện tại của SĐT (sau lần điểm danh này).</summary>
        public int LoyaltyPoints { get; set; }
    }

    /// <summary>
    /// Body request điểm danh hàng ngày. Quán online nên điểm danh chỉ cần
    /// SĐT — không cần GPS hay quét QR tại quầy.
    /// </summary>
    public class CheckInRequestModel
    {
        /// <summary>SĐT khách dùng để điểm danh và cộng điểm loyalty.</summary>
        public string Phone { get; set; }
    }
}
