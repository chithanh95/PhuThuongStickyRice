using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API chiến dịch phát voucher hàng loạt (item 132, admin):
    /// liệt kê đối tượng nhận (audiences) và phát hành mã cá nhân theo SĐT
    /// (generate). bug-hunt 2026-10-20: controller này từng thiếu hoàn toàn —
    /// frontend admin (AdminCommerceService) gọi api/voucher-campaigns/*
    /// nhưng không có route backend nên mọi thao tác đều 404.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/voucher-campaigns")]
    [ApiController]
    public class VoucherCampaignsController : ControllerBase
    {
        private readonly IVoucherCampaignAppService _campaigns;

        public VoucherCampaignsController(IVoucherCampaignAppService campaigns)
        {
            _campaigns = campaigns;
        }

        /// <summary>
        /// Admin: danh sách 3 nhóm đối tượng chiến dịch kèm số lượng người
        /// nhận ước tính (không mua 30+ ngày, theo hạng, tất cả khách).
        /// </summary>
        /// <param name="tier">Hạng thành viên cho nhóm theo hạng ("Bạc"/"Vàng"); trống thì mặc định hạng Vàng.</param>
        [HttpGet("audiences")]
        public async Task<ActionResult> GetAudiences(
            [FromQuery] string tier,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _campaigns.GetAudiencesAsync(tier ?? string.Empty));
        }

        /// <summary>
        /// Admin: phát hành 1 mã giảm giá cá nhân cho mỗi SĐT trong đối tượng
        /// đã chọn (dùng 1 lần, gắn riêng SĐT đó).
        /// </summary>
        /// <param name="input">Đối tượng, mức giảm, tiền tố mã, số ngày hiệu lực.</param>
        [HttpPost("generate")]
        public async Task<ActionResult> Generate(
            [FromBody] CreateVoucherCampaignInput input,
            CancellationToken cancellationToken = default)
        {
            if (input == null)
            {
                return BadRequest(new { message = "Thiếu thông tin chiến dịch voucher." });
            }

            try
            {
                return Ok(await _campaigns.GenerateAsync(input));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
