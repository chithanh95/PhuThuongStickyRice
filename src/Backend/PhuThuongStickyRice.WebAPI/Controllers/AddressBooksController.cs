

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
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

            public AddressBooksController(IAddressBookAppService addresses)
            {
                _addresses = addresses;
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
            /// Public: xóa một địa chỉ trong sổ địa chỉ. Yêu cầu kèm SĐT chủ
            /// sở hữu để không xóa nhầm địa chỉ của khách khác.
            /// </summary>
            /// <param name="id">Id địa chỉ cần xóa.</param>
            /// <param name="phone">SĐT chủ sở hữu địa chỉ (xác thực quyền xóa).</param>
            /// <param name="cancellationToken">Token hủy request.</param>
            [AllowAnonymous]
            [HttpDelete("{id}")]
            public async Task<ActionResult> Delete(Guid id, [FromQuery] string phone,
                CancellationToken cancellationToken = default)
            {
                await _addresses.DeleteAsync(id, phone);
                return Ok();
            }
        }
}
