using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.AuditLogs
{
    /// <summary>
    /// Một dòng log ứng dụng (AppLog) hiển thị ở trang /admin/nhat-ky —
    /// dùng để tra cứu lỗi vận hành.
    /// </summary>
    public class AppLogDto
    {
        public Guid Id { get; set; }

        /// <summary>Thời điểm ghi log (giờ VN).</summary>
        public DateTime At { get; set; }

        /// <summary>Mức log: Information / Warning / Error.</summary>
        public string Level { get; set; }

        /// <summary>Nguồn ghi log (tên class/service).</summary>
        public string Source { get; set; }

        /// <summary>Nội dung thông điệp log.</summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Tham số lọc + phân trang danh sách log ứng dụng ở trang /admin/nhat-ky.
    /// </summary>
    public class GetAppLogsInput
    {
        /// <summary>Lọc theo mức log; null/rỗng = tất cả.</summary>
        public string Level { get; set; }

        /// <summary>Từ khóa tìm trong nội dung log và tên nguồn.</summary>
        public string SearchTerm { get; set; }

        /// <summary>Số dòng bỏ qua (phân trang).</summary>
        public int SkipCount { get; set; }

        /// <summary>Số dòng mỗi trang, mặc định 50.</summary>
        public int MaxResultCount { get; set; } = 50;
    }
}
