

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Customer;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Address book (item 119): public, keyed by customer phone.</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class AddressBooksController : ControllerBase
        {
            private readonly IAddressBookAppService _addresses;
            private readonly PhoneOtpService _phoneOtpService;

            public AddressBooksController(IAddressBookAppService addresses, PhoneOtpService phoneOtpService)
            {
                _addresses = addresses;
                _phoneOtpService = phoneOtpService;
            }

            /// <summary>
            /// Public: lấy sổ địa chỉ đã lưu của khách theo SĐT — checkout dùng
            /// để điền nhanh địa chỉ giao hàng, khách không cần đăng nhập.
            /// </summary>
            /// <param name="phone">SĐT khách hàng.</param>
            /// <param name="cancellationToken">Token hủy request.</param>
            [AllowAnonymous]
            [HttpGet]
            public async Task<ActionResult<List<AddressBookDto>>> GetByPhone(
                [FromQuery] string phone,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _addresses.GetByPhoneAsync(phone));
            }

            /// <summary>
            /// Public: lưu mới (hoặc cập nhật) một địa chỉ vào sổ địa chỉ của
            /// khách theo SĐT — dùng ở checkout để lần sau đặt nhanh hơn.
            /// </summary>
            /// <param name="dto">Địa chỉ cần lưu.</param>
            /// <param name="cancellationToken">Token hủy request.</param>
            [AllowAnonymous]
            [HttpPost]
            public async Task<ActionResult<AddressBookDto>> Save(
                [FromBody] SaveAddressDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _addresses.SaveAsync(dto));
            }

            /// <summary>
            /// Public: xóa một địa chỉ trong sổ địa chỉ. Ngoài SĐT chủ sở
            /// hữu, bắt buộc kèm mã OTP đã gửi về chính SĐT đó (purpose
            /// address-delete, xin mã qua POST api/phone-otp/send) — audit
            /// bảo mật 2026-10-20: trước đây chỉ cần biết SĐT + id là xóa
            /// được địa chỉ của người khác. Thiếu/sai mã trả 400 kèm thông
            /// báo tiếng Việt.
            /// </summary>
            /// <param name="id">Id địa chỉ cần xóa.</param>
            /// <param name="phone">SĐT chủ sở hữu địa chỉ (xác thực quyền xóa).</param>
            /// <param name="otpCode">Mã OTP 6 số đã gửi về SĐT chủ sở hữu.</param>
            /// <param name="cancellationToken">Token hủy request.</param>
            [AllowAnonymous]
            [HttpDelete("{id}")]
            public async Task<ActionResult> Delete(Guid id, [FromQuery] string phone,
                [FromQuery] string otpCode,
                CancellationToken cancellationToken = default)
            {
                var verify = await _phoneOtpService.VerifyAsync(
                    phone, PhoneOtpService.Purposes.AddressDelete, otpCode);
                if (!verify.Succeeded)
                {
                    return BadRequest(new { message = verify.ErrorMessage });
                }

                await _addresses.DeleteAsync(id, phone);
                return Ok();
            }
        }
}
