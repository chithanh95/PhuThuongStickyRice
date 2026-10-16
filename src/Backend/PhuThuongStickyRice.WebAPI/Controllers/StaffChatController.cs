using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Internal staff team chat (shift communication). Simple broadcast channel.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/staff-chat")]
    [ApiController]
    [Authorize(Roles = "Admin,Cashier,Kitchen")]
    public class StaffChatController : ControllerBase
    {
        private readonly StickyRiceDbContext _db;

        public StaffChatController(StickyRiceDbContext db)
        {
            _db = db;
        }

        public class StaffChatSendRequest
        {
            public string Message { get; set; }
        }

        /// <summary>Staff: get recent team chat messages (latest 100, oldest first).</summary>
        [HttpGet]
        public async Task<ActionResult> GetMessages(CancellationToken cancellationToken = default)
        {
            var messages = await _db.StaffChatMessages
                .OrderByDescending(m => m.SentAt)
                .Take(100)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.SenderName,
                    m.Message,
                    m.SentAt
                })
                .ToListAsync(cancellationToken);

            return Ok(messages);
        }

        /// <summary>Staff: send a message to the team channel.</summary>
        [HttpPost]
        public async Task<ActionResult> SendMessage(
            [FromBody] StaffChatSendRequest request,
            CancellationToken cancellationToken = default)
        {
            var message = (request?.Message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new { success = false, message = "Tin nhắn không được để trống." });
            }

            if (message.Length > StaffChatMessage.MessageMaxLength)
            {
                message = message[..StaffChatMessage.MessageMaxLength];
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            Guid.TryParse(userIdStr, out var senderId);
            var senderName = User.FindFirstValue(ClaimTypes.Name)
                ?? User.Identity?.Name
                ?? "Nhân viên";

            _db.StaffChatMessages.Add(new StaffChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                SenderName = senderName,
                Message = message,
                SentAt = DateTime.UtcNow,
                CreatedDateTime = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true });
        }
    }
}
