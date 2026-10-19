using PhuThuongStickyRice.Application.Zalo;
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
    /// Third-party integration status + test endpoints (Zalo OA, ...).
    /// All admin-only.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class IntegrationsController : ControllerBase
    {
        private readonly ZaloOaOptions _zaloOptions;
        private readonly IZaloNotificationService _zalo;

        public IntegrationsController(
            ZaloOaOptions zaloOptions,
            IZaloNotificationService zalo)
        {
            _zaloOptions = zaloOptions;
            _zalo = zalo;
        }

        /// <summary>Zalo OA configuration status (token is masked).</summary>
        [HttpGet("zalo")]
        public ActionResult GetZaloStatus(CancellationToken cancellationToken = default)
        {
            return Ok(new
            {
                configured = _zaloOptions.IsConfigured,
                oaId = Mask(_zaloOptions.OaId),
                accessToken = Mask(_zaloOptions.AccessToken),
                note = "Điền AccessToken và OaId thật vào appsettings (mục ZaloOa) để bật gửi tin nhắn. Xem README mục \"Zalo OA\"."
            });
        }

        public sealed class ZaloTestRequestModel
        {
            public string Phone { get; set; }

            public string Message { get; set; }
        }

        /// <summary>
        /// Sends a test message through the Zalo service. With the default
        /// "xxx" placeholders this only writes a "would send" log line.
        /// </summary>
        [HttpPost("zalo/test")]
        public async Task<ActionResult> TestZalo(
            [FromBody] ZaloTestRequestModel model,
            CancellationToken cancellationToken = default)
        {
            var message = string.IsNullOrWhiteSpace(model?.Message)
                ? "Xôi Phú Thượng: đây là tin nhắn kiểm tra tích hợp Zalo OA."
                : model.Message;
            await _zalo.SendTextMessageAsync(model?.Phone ?? "0900000000", message);
            return Ok(new { sent = _zaloOptions.IsConfigured, stubbed = !_zaloOptions.IsConfigured });
        }

        private static string Mask(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "xxx")
            {
                return "xxx (chưa cấu hình)";
            }

            return value.Length <= 4
                ? "****"
                : string.Concat(new string('*', value.Length - 4), value.AsSpan(value.Length - 4));
        }
    }
}
