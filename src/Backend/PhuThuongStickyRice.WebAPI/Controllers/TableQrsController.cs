

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 95: table QR codes (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    /// <summary>
    /// API mã QR dán trên bàn quán (khách quét để mở shop/đặt món tại bàn).
    /// Tạo/làm mới QR và tải file PNG để in; xóa nhãn QR khỏi hệ thống.
    /// </summary>
    public class TableQrsController : ControllerBase
    {
        private readonly ITableQrAppService _tableQrs;

        public TableQrsController(ITableQrAppService tableQrs)
        {
            _tableQrs = tableQrs;
        }

        /// <summary>Lấy toàn bộ danh sách nhãn QR bàn đã tạo (quản lý ở admin).</summary>
        /// <param name="cancellationToken">Token hủy request.</param>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _tableQrs.GetListAsync());
        }

        /// <summary>Tạo mới (hoặc làm mới) mã QR cho 1 nhãn bàn.</summary>
        /// <param name="dto">Thông tin nhãn bàn cần tạo QR.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [HttpPost]
        public async Task<ActionResult> Generate(
            [FromBody] CreateTableQrDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _tableQrs.GenerateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tải file PNG của mã QR đã lưu để in dán bàn. Public (không cần
        /// đăng nhập) vì QR in trên bàn chỉ mã hóa URL shop công khai.
        /// </summary>
        /// <param name="id">Id nhãn QR bàn.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpGet("{id:guid}/png")]
        public async Task<ActionResult> DownloadPng(Guid id, CancellationToken cancellationToken = default)
        {
            var png = await _tableQrs.RenderPngAsync(id);
            return File(png, "image/png");
        }

        /// <summary>Xóa 1 nhãn QR bàn khỏi hệ thống.</summary>
        /// <param name="id">Id nhãn QR cần xóa.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _tableQrs.DeleteAsync(id);
            return NoContent();
        }
    }
}
