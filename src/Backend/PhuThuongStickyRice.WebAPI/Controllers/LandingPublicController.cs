using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Public landing-page data: reviews, stats, gallery, promo subscription.
    /// No authentication required; protected by the default rate limiter.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/landing")]
    [ApiController]
    public class LandingPublicController : ControllerBase
    {
        private readonly StickyRiceDbContext _db;

        public LandingPublicController(StickyRiceDbContext db)
        {
            _db = db;
        }

        /// <summary>Public: latest visible customer reviews (max 20).</summary>
        [AllowAnonymous]
        [HttpGet("reviews")]
        public async Task<ActionResult> GetReviews(CancellationToken cancellationToken = default)
        {
            var reviews = await _db.OrderReviews
                .Where(r => !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .Select(r => new
                {
                    displayName = MaskPhone(r.CustomerPhone),
                    rating = r.Rating,
                    comment = r.Comment,
                    photoUrl = r.PhotoUrl,
                    createdAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(reviews);
        }

        /// <summary>Public: headline stats for the landing page.</summary>
        [AllowAnonymous]
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats(CancellationToken cancellationToken = default)
        {
            var totalOrders = await _db.Orders.CountAsync(cancellationToken);

            var visibleRatings = _db.OrderReviews.Where(r => !r.IsHidden);
            var reviewCount = await visibleRatings.CountAsync(cancellationToken);
            var avgRating = reviewCount == 0
                ? 0
                : Math.Round(await visibleRatings.AverageAsync(r => (double)r.Rating, cancellationToken), 1);

            return Ok(new
            {
                totalOrders,
                avgRating,
                reviewCount
            });
        }

        /// <summary>Public: approved customer photos (max 12).</summary>
        [AllowAnonymous]
        [HttpGet("gallery")]
        public async Task<ActionResult> GetGallery(CancellationToken cancellationToken = default)
        {
            var photos = await _db.CustomerPhotos
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .Select(p => new
                {
                    imageUrl = p.ImageUrl,
                    caption = p.Caption,
                    name = p.Name
                })
                .ToListAsync(cancellationToken);

            return Ok(photos);
        }

        public class SubscribeRequest
        {
            public string Phone { get; set; }
        }

        /// <summary>Public: subscribe a VN phone number for promo notifications.</summary>
        [AllowAnonymous]
        [HttpPost("subscribe")]
        public async Task<ActionResult> Subscribe(
            [FromBody] SubscribeRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();

            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var exists = await _db.PromoSubscribers
                .AnyAsync(s => s.Phone == phone, cancellationToken);

            if (!exists)
            {
                _db.PromoSubscribers.Add(new PromoSubscriber
                {
                    Id = Guid.NewGuid(),
                    Phone = phone,
                    CreatedAt = DateTime.UtcNow,
                    Source = "landing"
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Ok(new { success = true });
        }

        private static bool IsValidVnPhone(string phone) =>
            !string.IsNullOrWhiteSpace(phone)
            && Regex.IsMatch(phone.Trim(), @"^(0)(3|5|7|8|9)\d{8}$");

        // ============ Landing engagement: lucky spin (item 28) ============

        public class SpinRequest
        {
            public string Phone { get; set; }
        }

        /// <summary>
        /// Public: spin the lucky wheel once per phone per day.
        /// Prizes: 5k (40%), 10k (30%), 15k (20%), 20k (10%).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("spin")]
        public async Task<ActionResult> Spin(
            [FromBody] SpinRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();

            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var today = DateTime.UtcNow.Date;
            var alreadySpun = await _db.LuckySpins
                .AnyAsync(s => s.Phone == phone && s.SpinDate == today, cancellationToken);

            if (alreadySpun)
            {
                return Ok(new { success = false, message = "Hôm nay bạn đã quay rồi, hẹn mai quay tiếp nhé!" });
            }

            // Weighted random prize.
            var roll = Random.Shared.NextDouble();
            var (prizeLabel, prizeValue) = roll switch
            {
                < 0.40 => ("5k", 5000),
                < 0.70 => ("10k", 10000),
                < 0.90 => ("15k", 15000),
                _ => ("20k", 20000),
            };

            var voucherCode = await GenerateUniqueVoucherCodeAsync(cancellationToken);

            _db.LuckySpins.Add(new LuckySpin
            {
                Id = Guid.NewGuid(),
                Phone = phone,
                SpinDate = today,
                PrizeLabel = prizeLabel,
                PrizeValue = prizeValue,
                VoucherCode = voucherCode,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, prizeLabel, prizeValue, voucherCode });
        }

        private async Task<string> GenerateUniqueVoucherCodeAsync(CancellationToken cancellationToken)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var code = "XOI" + new string(Enumerable.Range(0, 6)
                    .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
                if (!await _db.LuckySpins.AnyAsync(s => s.VoucherCode == code, cancellationToken))
                {
                    return code;
                }
            }

            // Fallback: virtually impossible, but never loop forever.
            return "XOI" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        }

        // ============ Landing engagement: referral (item 29) ============

        public class ReferralRequest
        {
            public string ReferrerPhone { get; set; }
            public string ReferredPhone { get; set; }
        }

        /// <summary>Public: record a customer referral (friend invitation).</summary>
        [AllowAnonymous]
        [HttpPost("referral")]
        public async Task<ActionResult> AddReferral(
            [FromBody] ReferralRequest request,
            CancellationToken cancellationToken = default)
        {
            var referrer = (request?.ReferrerPhone ?? string.Empty).Trim();
            var referred = (request?.ReferredPhone ?? string.Empty).Trim();

            if (!IsValidVnPhone(referrer) || !IsValidVnPhone(referred))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            if (referrer == referred)
            {
                return BadRequest(new { success = false, message = "Không thể tự giới thiệu chính mình." });
            }

            var exists = await _db.Referrals
                .AnyAsync(r => r.ReferrerPhone == referrer && r.ReferredPhone == referred, cancellationToken);

            if (!exists)
            {
                _db.Referrals.Add(new Referral
                {
                    Id = Guid.NewGuid(),
                    ReferrerPhone = referrer,
                    ReferredPhone = referred,
                    RewardAmount = 10000,
                    IsRewarded = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Ok(new { success = true });
        }

        // ============ Landing engagement: quick rating (item 36) ============

        public class QuickRatingRequest
        {
            public int Rating { get; set; }
            public Guid? ProductId { get; set; }
        }

        /// <summary>Public: one-tap rating, no login required.</summary>
        [AllowAnonymous]
        [HttpPost("quick-rating")]
        public async Task<ActionResult> AddQuickRating(
            [FromBody] QuickRatingRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null || request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao." });
            }

            _db.QuickRatings.Add(new QuickRating
            {
                Id = Guid.NewGuid(),
                Rating = request.Rating,
                ProductId = request.ProductId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true });
        }

        // ============ Landing engagement: live chat (item 37) ============

        public class ChatSendRequest
        {
            public string SessionId { get; set; }
            public string Message { get; set; }
        }

        /// <summary>Public: send a customer chat message.</summary>
        [AllowAnonymous]
        [HttpPost("chat")]
        public async Task<ActionResult> SendChatMessage(
            [FromBody] ChatSendRequest request,
            CancellationToken cancellationToken = default)
        {
            var sessionId = (request?.SessionId ?? string.Empty).Trim();
            var message = (request?.Message ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > ChatMessage.SessionIdMaxLength)
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
                Sender = ChatMessage.SenderCustomer,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true });
        }

        /// <summary>
        /// Public: get the message history of a chat session (max 50).
        /// Marks admin messages as read once the customer fetches them.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("chat/{sessionId}")]
        public async Task<ActionResult> GetChatMessages(
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
                .Take(50)
                .ToListAsync(cancellationToken);

            var unreadAdmin = messages
                .Where(m => m.Sender == ChatMessage.SenderAdmin && !m.IsRead)
                .ToList();

            if (unreadAdmin.Count > 0)
            {
                foreach (var m in unreadAdmin)
                {
                    m.IsRead = true;
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            return Ok(messages.Select(m => new
            {
                sender = m.Sender,
                message = m.Message,
                createdAt = m.CreatedAt
            }));
        }

        /// <summary>
        /// Masks a phone number for public display: "0912345678" -&gt; "091***78".
        /// </summary>
        private static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "Khách hàng";
            }

            var digits = new string(phone.Trim().Where(char.IsDigit).ToArray());
            if (digits.Length < 5)
            {
                return "Khách hàng";
            }

            return digits.Substring(0, 3) + "***" + digits.Substring(digits.Length - 2);
        }
    }
}
