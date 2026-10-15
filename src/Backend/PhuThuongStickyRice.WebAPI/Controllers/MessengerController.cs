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
    public class MessengerController : ControllerBase
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

        /// <summary>Receives Messenger events (messages, etc.).</summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Receive()
        {
            JsonDocument payload;
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                payload = JsonDocument.Parse(body);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON on Messenger webhook.");
                return BadRequest();
            }

            using (payload)
            {
                await _chat.HandleWebhookAsync(payload);
            }

            // Always 200 so Facebook does not retry endlessly.
            return Ok();
        }
    }
}
