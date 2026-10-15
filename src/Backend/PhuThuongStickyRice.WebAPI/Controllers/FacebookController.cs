using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuThuongStickyRice.Application.Facebook;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 130: auto-post new products to the Facebook Page feed.
    /// Admin-only. Works in "waiting for configuration" mode when the
    /// Page ID / Page token is missing (attempts are logged as Skipped).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/admin/facebook")]
    [ApiController]
    public class FacebookController : ControllerBase
    {
        private readonly IFacebookPostService _facebook;

        public FacebookController(IFacebookPostService facebook)
        {
            _facebook = facebook;
        }

        /// <summary>
        /// Whether auto-posting is configured (Page ID + Page token).
        /// The product form defaults its "Đăng lên Facebook" checkbox from this.
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<FacebookStatusDto>> GetStatus()
        {
            return Ok(await _facebook.GetStatusAsync());
        }

        /// <summary>Recent auto-post history, newest first.</summary>
        [HttpGet("posts")]
        public async Task<ActionResult<List<FacebookPostLogDto>>> GetPosts(
            [FromQuery] int maxCount = 50)
        {
            return Ok(await _facebook.GetHistoryAsync(maxCount));
        }

        /// <summary>
        /// Publishes a product announcement to the Page feed now
        /// (used after creating/enabling a product, or manually per row).
        /// </summary>
        [HttpPost("publish-product/{id}")]
        public async Task<ActionResult<FacebookPublishResultDto>> PublishProduct(Guid id)
        {
            return Ok(await _facebook.PublishProductAsync(id));
        }
    }
}
