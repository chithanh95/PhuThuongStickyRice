using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Địa chỉ đã lưu của khách hàng đăng nhập. Mọi endpoint đều gắn phạm vi
    /// sở hữu phía server qua JWT identity — khách chỉ đọc/sửa/xóa được địa
    /// chỉ của chính mình, không chạm được địa chỉ người khác.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/customer/addresses")]
    [ApiController]
    [Authorize]
    public class CustomerAddressesController : ControllerBase
    {
        private readonly ICustomerAddressAppService _addresses;

        public CustomerAddressesController(ICustomerAddressAppService addresses)
        {
            _addresses = addresses;
        }

        /// <summary>
        /// Khách: danh sách địa chỉ đã lưu của chính mình (trang hồ sơ/checkout).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<CustomerAddressDto>>> GetMine(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.GetMyAsync());
        }

        /// <summary>
        /// Khách: thêm địa chỉ mới cho tài khoản của mình.
        /// </summary>
        /// <param name="dto">Tên người nhận, SĐT, địa chỉ chi tiết.</param>
        [HttpPost]
        public async Task<ActionResult<CustomerAddressDto>> Create(
            [FromBody] CreateCustomerAddressDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.CreateAsync(dto));
        }

        /// <summary>
        /// Khách: cập nhật địa chỉ của mình. Service kiểm tra id thuộc sở hữu
        /// của người gọi, tránh sửa địa chỉ của người khác.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CustomerAddressDto>> Update(
            Guid id,
            [FromBody] UpdateCustomerAddressDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.UpdateAsync(id, dto));
        }

        /// <summary>
        /// Khách: xóa 1 địa chỉ của mình.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await _addresses.DeleteAsync(id);
            return Ok();
        }

        /// <summary>
        /// Khách: đặt 1 địa chỉ làm mặc định (checkout tự chọn sẵn).
        /// Địa chỉ cũ đang mặc định tự bị bỏ cờ.
        /// </summary>
        [HttpPost("{id}/default")]
        public async Task<ActionResult> SetDefault(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await _addresses.SetDefaultAsync(id);
            return Ok();
        }
    }
}
