

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Orders;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 82: quản lý shipper giao hàng (Admin/Cashier). Lỗi nghiệp vụ
    /// (DomainException) được dịch thành BadRequest với message tiếng Việt
    /// để màn hình admin hiển thị trực tiếp, không phải try/catch ở client.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin,Cashier")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ShippersController : ControllerBase
    {
        private readonly IShipperAppService _shippers;

        public ShippersController(IShipperAppService shippers)
        {
            _shippers = shippers;
        }

        /// <summary>
        /// Admin/Cashier: danh sách shipper. Lọc chỉ shipper đang hoạt động
        /// khi gán shipper cho đơn hàng.
        /// </summary>
        /// <param name="activeOnly">true: chỉ shipper đang hoạt động.</param>
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] bool activeOnly = false,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _shippers.GetListAsync(activeOnly));
        }

        /// <summary>
        /// Admin/Cashier: thêm shipper mới. SĐT trùng thì trả 400 (DomainException).
        /// </summary>
        /// <param name="dto">Tên, SĐT shipper.</param>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateShipperDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _shippers.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin/Cashier: cập nhật thông tin shipper.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] UpdateShipperDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _shippers.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin/Cashier: xóa shipper (trả 204 khi thành công).
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _shippers.DeleteAsync(id);
            return NoContent();
        }
    }
}
