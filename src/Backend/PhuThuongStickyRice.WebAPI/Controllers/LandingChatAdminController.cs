using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Admin endpoints for the landing-page live chat (item 37).
    /// Lists chat sessions, reads a session's messages, and lets staff reply.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/admin/landing-chat")]
    [ApiController]
    public class LandingChatAdminController : ControllerBase
    {
        private readonly StickyRiceDbContext _db;

        public LandingChatAdminController(StickyRiceDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Admin: chat sessions ordered by latest activity, with unread counts.
        /// </summary>
        [HttpGet("sessions")]
        public async Task<ActionResult> GetSessions(CancellationToken cancellationToken = default)
        {
            var sessions = await _db.ChatMessages
                .GroupBy(m => m.SessionId)
                .Select(g => new
                {
                    sessionId = g.Key,
                    lastMessage = g.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Message).FirstOrDefault(),
                    lastSender = g.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Sender).FirstOrDefault(),
                    lastAt = g.Max(m => m.CreatedAt),
                    unreadCount = g.Count(m => m.Sender == ChatMessage.SenderCustomer && !m.IsRead)
                })
                .OrderByDescending(s => s.lastAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            return Ok(sessions);
        }

        /// <summary>
        /// Admin: full message history of one session (max 200).
        /// Marks customer messages as read once staff opens the session.
        /// </summary>
        [HttpGet("messages/{sessionId}")]
        public async Task<ActionResult> GetMessages(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            sessionId = (sessionId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { success = false, message = "Phiên chat không hợp lệ." });
            }

            var messages = await _db.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .Take(200)
                .ToListAsync(cancellationToken);

            var unreadCustomer = messages
                .Where(m => m.Sender == ChatMessage.SenderCustomer && !m.IsRead)
                .ToList();

            if (unreadCustomer.Count > 0)
            {
                foreach (var m in unreadCustomer)
                {
                    m.IsRead = true;
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            return Ok(messages.Select(m => new
            {
                id = m.Id,
                sender = m.Sender,
                message = m.Message,
                isRead = m.IsRead,
                createdAt = m.CreatedAt
            }));
        }

        public class ReplyRequest
        {
            public string SessionId { get; set; }
            public string Message { get; set; }
        }

        /// <summary>Admin: reply to a chat session as staff.</summary>
        [HttpPost("reply")]
        public async Task<ActionResult> Reply(
            [FromBody] ReplyRequest request,
            CancellationToken cancellationToken = default)
        {
            var sessionId = (request?.SessionId ?? string.Empty).Trim();
            var message = (request?.Message ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { success = false, message = "Phiên chat không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new { success = false, message = "Tin nhắn không được để trống." });
            }

            if (message.Length > ChatMessage.MessageMaxLength)
            {
                message = message[..ChatMessage.MessageMaxLength];
            }

            _db.ChatMessages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Sender = ChatMessage.SenderAdmin,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true });
        }
    }
}
