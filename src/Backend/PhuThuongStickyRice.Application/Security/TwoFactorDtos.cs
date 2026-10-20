using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Security
{
        /// <summary>
        /// Thông tin cài đặt 2FA trả về cho admin khi bắt đầu bật 2FA: quét QR
        /// bằng app Authenticator rồi nhập mã 6 số để xác nhận.
        /// </summary>
        public class TwoFactorSetupDto
        {
            /// <summary>Chuỗi secret Base32 (cũng đã nhúng trong QR) — chỉ hiển thị 1 lần.</summary>
            public string Secret { get; set; }

            /// <summary>URI otpauth:// để app Authenticator đăng ký.</summary>
            public string OtpAuthUri { get; set; }

            /// <summary>Ảnh QR dạng PNG data URL để quét.</summary>
            public string QrDataUrl { get; set; }

            /// <summary>True nếu tài khoản đã bật 2FA trước đó.</summary>
            public bool IsEnabled { get; set; }
        }

        /// <summary>
        /// Body xác thực 2FA: mã 6 số từ app Authenticator. Dùng cho cả bước
        /// xác nhận khi bật 2FA và bước đăng nhập.
        /// </summary>
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
