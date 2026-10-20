using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.AuditLogs
{
    /// <summary>
    /// Một bản ghi đăng nhập (thành công hoặc thất bại) — dùng để rà soát
    /// bảo mật: phát hiện đăng nhập lạ, brute-force.
    /// </summary>
    public class LoginAuditDto
    {
        public Guid Id { get; set; }

        /// <summary>Tên đăng nhập đã thử.</summary>
        public string UserName { get; set; }

        /// <summary>Thời điểm đăng nhập (giờ VN).</summary>
        public DateTime At { get; set; }

        /// <summary>IP của client thực hiện đăng nhập.</summary>
        public string IpAddress { get; set; }

        /// <summary>True = đăng nhập thành công.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Lý do thất bại (sai mật khẩu, tài khoản bị khóa...); null nếu thành công.</summary>
        public string FailureReason { get; set; }
    }
}
