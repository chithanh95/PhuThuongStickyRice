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
