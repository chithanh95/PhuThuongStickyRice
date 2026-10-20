using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Application.Messenger;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Facebook Messenger webhook for the rule-based ordering chatbot (item 53).
    ///
    /// Subscription (Facebook App dashboard &gt; Messenger &gt; Webhooks):
    ///   Callback URL: https://&lt;domain&gt;/api/webhooks/messenger
    ///   Verify token: the string saved in Admin &gt; Cài đặt cửa hàng &gt;
    ///                 Chatbot Messenger &gt; Verify Token
    ///   Fields: messages
    ///
    /// You also need a Page Access Token (same settings screen). Without it
    /// the webhook still verifies and incoming messages are logged, but no
    /// replies are sent.
    /// </summary>
    [Route("api/webhooks/messenger")]
    [ApiController]
    public partial class MessengerController : ControllerBase
    {
        private readonly IMessengerChatService _chat;
        private readonly ILogger<MessengerController> _logger;

        public MessengerController(
            IMessengerChatService chat,
            ILogger<MessengerController> logger)
        {
            _chat = chat;
            _logger = logger;
        }

        /// <summary>Webhook verification handshake (called by Facebook).</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Verify(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.verify_token")] string verifyToken,
            [FromQuery(Name = "hub.challenge")] string challenge)
        {
            var result = await _chat.VerifyWebhookAsync(mode, verifyToken, challenge);
            if (result == null)
            {
                return Forbid();
            }
            return Content(result, "text/plain");
        }

        /// <summary>
        /// Receives Messenger events (messages, etc.). The
        /// X-Hub-Signature-256 header is verified against the configured
        /// Facebook App Secret before the payload is processed.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Receive()
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            var bodyBytes = ms.ToArray();

            JsonDocument payload;
            try
            {
                payload = JsonDocument.Parse(bodyBytes);
            }
            catch (JsonException ex)
            {
                LogInvalidWebhookJson(_logger, ex);
                return BadRequest();
            }

            // bug-hunt 2026-10-20 (run 05:45): verify chữ ký chống giả mạo
            // callback (kẻ lạ POST payload giả có thể điều khiển bot tạo đơn
            // thật, bỏ qua reCAPTCHA). Không có secret cấu hình -> service
            // từ chối (fail-closed, 401) thay vì cho qua như trước.
            var signature = Request.Headers["X-Hub-Signature-256"].ToString();

            // [codequality-practices 2026-10-20] JsonDocument thuê bộ nhớ từ
            // shared pool nên PHẢI dispose trên mọi đường đi: trước đây dùng
            // `using (payload)` đặt SAU bước verify chữ ký → request bị từ
            // chối (Unauthorized) return sớm, pool không được trả bộ nhớ.
            // Gom cả verify + xử lý vào trong using để mọi nhánh return
            // (BadRequest/Unauthorized/Ok) đều giải phóng — hành vi xử lý
            // webhook giữ nguyên.
            using (payload)
            {
                if (!await _chat.VerifyWebhookSignatureAsync(bodyBytes, signature))
                {
                    return Unauthorized();
                }

                await _chat.HandleWebhookAsync(payload);
            }

            // Always 200 so Facebook does not retry endlessly.
            return Ok();
        }

        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated
        // (tránh cấp phát params array + boxing mỗi lần log).

        /// <summary>Log khi webhook Messenger nhận payload JSON không hợp lệ.</summary>
        [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid JSON on Messenger webhook.")]
        private static partial void LogInvalidWebhookJson(ILogger logger, Exception ex);
    }
}
