using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Staff
{
    /// <summary>
    /// 1 dòng nhật ký hoạt động của nhân viên (Item 156): ai đã làm gì, lúc
    /// nào — dùng để đối soát thao tác trên đơn hàng, mã giảm giá, phân quyền.
    /// </summary>
    public class StaffActivityLogDto
    {
        /// <summary>Id dòng log.</summary>
        public Guid Id { get; set; }

        /// <summary>Id tài khoản nhân viên thực hiện; null với hành động hệ thống.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Tên đăng nhập của người thực hiện.</summary>
        public string UserName { get; set; }

        /// <summary>Khóa máy của hành động (vd "staff.create", xem StaffActivityActions).</summary>
        public string Action { get; set; }

        /// <summary>Nhãn tiếng Việt của hành động (vd "Tạo tài khoản nhân viên").</summary>
        public string ActionLabel { get; set; }

        /// <summary>Chi tiết hành động (vd "Tạo tài khoản thu1 vai trò Cashier.").</summary>
        public string Detail { get; set; }

        /// <summary>Thời điểm ghi log.</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Bộ lọc + phân trang cho màn hình nhật ký hoạt động nhân viên.
    /// </summary>
    public class GetStaffActivityLogsInput
    {
        /// <summary>Lọc theo tài khoản nhân viên; null = tất cả.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Lọc theo khóa hành động (vd "order.cancel"); null = tất cả.</summary>
        public string Action { get; set; }

        /// <summary>Lọc từ thời điểm này trở đi; null = không giới hạn.</summary>
        public DateTime? From { get; set; }

        /// <summary>Lọc đến thời điểm này; null = không giới hạn.</summary>
        public DateTime? To { get; set; }

        /// <summary>Số dòng bỏ qua (phân trang).</summary>
        public int SkipCount { get; set; }

        /// <summary>Số dòng lấy mỗi trang (mặc định 50).</summary>
        public int MaxResultCount { get; set; } = 50;
    }

    /// <summary>
    /// Kết quả trang nhật ký hoạt động: tổng số dòng + các dòng trong trang
    /// + danh sách khóa hành động để vẽ bộ lọc dropdown.
    /// </summary>
    public class StaffActivityLogListDto
    {
        /// <summary>Tổng số dòng log khớp bộ lọc (để tính số trang).</summary>
        public long TotalCount { get; set; }

        /// <summary>Các dòng log trong trang hiện tại.</summary>
        public List<StaffActivityLogDto> Items { get; set; } = new List<StaffActivityLogDto>();

        /// <summary>Các khóa hành động có trong dữ liệu (cho dropdown bộ lọc).</summary>
        public List<string> Actions { get; set; } = new List<string>();
    }
}
