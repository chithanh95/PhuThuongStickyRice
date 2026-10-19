using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Security
{
        public class TwoFactorSetupDto
        {
            /// <summary>Base32 secret; also embedded in the QR.</summary>
            public string Secret { get; set; }

            /// <summary>otpauth:// URI.</summary>
            public string OtpAuthUri { get; set; }

            /// <summary>PNG data URL of the QR code.</summary>
            public string QrDataUrl { get; set; }

            public bool IsEnabled { get; set; }
        }

        public class VerifyTwoFactorDto
        {
            [Required]
            [StringLength(6, MinimumLength = 6)]
            public string Code { get; set; }
        }
        // Bug-hunt 2026-10-19: đã xóa EnableTwoFactorDto (từng chứa Secret do client
        // gửi lên). Secret cài đặt 2FA giờ do server sinh và giữ trong cache chờ
        // xác thực — EnableAsync chỉ nhận mã 6 số qua VerifyTwoFactorDto.
}
