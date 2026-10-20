using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API xin mã OTP theo SĐT cho các thao tác nhạy cảm phía khách (xóa
    /// địa chỉ, đổi ngày sinh, xem chi tiết lịch sử đơn...). Việc kiểm tra
    /// mã nằm ở từng endpoint đích thông qua
    /// <see cref="PhoneOtpService.VerifyAsync"/> — controller này chỉ phát
    /// mã. Chi tiết chuẩn OTP (TTL 5 phút, 3 lần thử, dùng 1 lần) xem
    /// <see cref="PhoneOtpService"/>.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/phone-otp")]
    [ApiController]
    public class PhoneOtpController : ControllerBase
    {
        private readonly PhoneOtpService _phoneOtp;

        /// <summary>Khởi tạo controller phát mã OTP theo SĐT.</summary>
        public PhoneOtpController(PhoneOtpService phoneOtp)
        {
            _phoneOtp = phoneOtp;
        }

        /// <summary>
        /// Public: gửi mã OTP 6 số về SĐT cho 1 mục đích xác thực
        /// (address-delete / birthday-change / order-history /
        /// checkout-spend). SĐT sai định dạng, mục đích lạ hoặc xin mã quá
        /// 3 lần/giờ trả 400 kèm thông báo tiếng Việt.
        /// </summary>
        /// <param name="request">SĐT nhận mã và mục đích xác thực.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpPost("send")]
        public async Task<ActionResult> Send(
            [FromBody] SendPhoneOtpRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _phoneOtp.SendAsync(request?.Phone, request?.Purpose, cancellationToken);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Đã gửi mã OTP về số điện thoại của bạn. Mã có hiệu lực 5 phút." });
        }

        /// <summary>Body xin mã OTP: SĐT nhận mã + mục đích xác thực.</summary>
        public class SendPhoneOtpRequest
        {
            /// <summary>SĐT nhận OTP (định dạng VN 10 số bắt đầu bằng 0).</summary>
            public string Phone { get; set; }

            /// <summary>
            /// Mục đích xác thực — một trong các hằng
            /// <see cref="PhoneOtpService.Purposes"/>.
            /// </summary>
            public string Purpose { get; set; }
        }
    }
}
