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

        public class EnableTwoFactorDto : VerifyTwoFactorDto
        {
            [Required]
            public string Secret { get; set; }
        }
}
