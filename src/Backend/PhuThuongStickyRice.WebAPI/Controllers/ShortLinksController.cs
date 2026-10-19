

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Short links (item 116): admin CRUD + public create (quick-view "🔗 Link ngắn").</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class ShortLinksController : ControllerBase
        {
            private readonly IShortLinkAppService _links;

            public ShortLinksController(IShortLinkAppService links)
            {
                _links = links;
            }

            [Authorize(Roles = "Admin")]
            [HttpGet]
            public async Task<ActionResult<List<ShortLinkDto>>> GetList(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _links.GetListAsync());
            }

            [AllowAnonymous]
            [HttpPost]
            public async Task<ActionResult<ShortLinkDto>> Create(
                [FromBody] CreateShortLinkDto dto,
                CancellationToken cancellationToken = default)
            {
                // Chặn phishing: endpoint public chỉ được rút gọn URL nội bộ
                // (relative) hoặc cùng host với website — không cho tạo
                // short-link dạng https://shop/s/xxxxxx trỏ ra site lạ.
                if (!IsSameHostUrl(dto?.TargetUrl))
                {
                    return BadRequest("Chỉ cho phép rút gọn liên kết thuộc website của quán.");
                }

                return Ok(await _links.CreateAsync(dto));
            }

            [Authorize(Roles = "Admin")]
            [HttpDelete("{id}")]
            public async Task<ActionResult> Delete(Guid id,
                CancellationToken cancellationToken = default)
            {
                await _links.DeleteAsync(id);
                return Ok();
            }

            /// <summary>
            /// True khi URL là nội bộ (relative) hoặc tuyệt đối cùng host với
            /// request hiện tại và scheme http/https. URL rỗng để [Required] /
            /// [Url] trên DTO báo lỗi chuẩn.
            /// </summary>
            private bool IsSameHostUrl(string targetUrl)
            {
                if (string.IsNullOrWhiteSpace(targetUrl))
                {
                    return true;
                }

                var trimmed = targetUrl.Trim();
                if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                {
                    return true; // URL tương đối = nội bộ
                }

                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                {
                    return false;
                }

                return string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase);
            }
        }
}
