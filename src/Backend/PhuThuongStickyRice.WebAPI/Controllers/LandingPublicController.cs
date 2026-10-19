using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        // Lucky-wheel prize table: label, value (VND), cumulative probability.
        // Keep the probabilities sorted ascending; PickPrize returns the
        // first prize whose cumulative chance exceeds the roll.
        private static readonly (string Label, int Value, double CumulativeChance)[] SpinPrizes =
        {
            ("5k", 5000, 0.40),
            ("10k", 10000, 0.70),
            ("15k", 15000, 0.90),
            ("20k", 20000, 1.00),
        };

        private const string VoucherCodePrefix = "XOI";
        private const int VoucherCodeRandomLength = 6;
        private const int VoucherCodeMaxAttempts = 10;

        private static (string Label, int Value) PickPrize(double roll)
        {
            foreach (var prize in SpinPrizes)
            {
                if (roll < prize.CumulativeChance)
                {
                    return (prize.Label, prize.Value);
                }
            }

            return (SpinPrizes[^1].Label, SpinPrizes[^1].Value);
        }

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
            var (prizeLabel, prizeValue) = PickPrize(roll);

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
            for (var attempt = 0; attempt < VoucherCodeMaxAttempts; attempt++)
            {
                var code = VoucherCodePrefix + new string(Enumerable.Range(0, VoucherCodeRandomLength)
                    .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
                if (!await _db.LuckySpins.AnyAsync(s => s.VoucherCode == code, cancellationToken))
                {
                    return code;
                }
            }

            // Fallback: virtually impossible, but never loop forever.
            return VoucherCodePrefix + Guid.NewGuid().ToString("N")[..VoucherCodeRandomLength].ToUpperInvariant();
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

        // ============ Sinh nhật tặng quà (item 43) ============

        public class BirthdayGiftRequest
        {
            public string Phone { get; set; }
            public DateTime? BirthDate { get; set; }
        }

        /// <summary>
        /// Public: lưu ngày sinh; nếu sinh nhật trong 7 ngày tới thì tặng mã quà.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("birthday-gift")]
        public async Task<ActionResult> BirthdayGift(
            [FromBody] BirthdayGiftRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var birthDate = request?.BirthDate?.Date;
            if (!birthDate.HasValue || birthDate.Value > DateTime.Today.AddDays(-1)
                || birthDate.Value < DateTime.Today.AddYears(-120))
            {
                return BadRequest(new { success = false, message = "Ngày sinh không hợp lệ." });
            }

            var account = await _db.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.Phone == phone, cancellationToken);
            if (account == null)
            {
                account = new LoyaltyAccount
                {
                    Id = Guid.NewGuid(),
                    Phone = phone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.LoyaltyAccounts.Add(account);
            }

            account.DateOfBirth = birthDate.Value;
            account.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            var daysUntil = DaysUntilBirthday(birthDate.Value, DateTime.Today);
            if (daysUntil <= 7)
            {
                var giftCode = "SN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
                return Ok(new
                {
                    success = true,
                    giftCode,
                    message = daysUntil == 0
                        ? "Chúc mừng sinh nhật! Quán tặng bạn mã quà sinh nhật."
                        : $"Sinh nhật bạn còn {daysUntil} ngày nữa — quán tặng trước mã quà sinh nhật!"
                });
            }

            return Ok(new { success = true, message = "Đã lưu ngày sinh. Đến sinh nhật quán sẽ tặng quà!" });
        }

        private static int DaysUntilBirthday(DateTime birthDate, DateTime today)
        {
            var next = new DateTime(today.Year, birthDate.Month, birthDate.Day);
            if (next < today)
            {
                next = next.AddYears(1);
            }
            // 29/2 -> 28/2 năm không nhuận.
            if (birthDate.Month == 2 && birthDate.Day == 29 && !DateTime.IsLeapYear(next.Year))
            {
                next = new DateTime(next.Year, 2, 28);
            }
            return (next - today).Days;
        }

        // ============ VIP Club (item 55) ============

        /// <summary>Public: trạng thái VIP theo SĐT (hạng Vàng hoặc chi tiêu &gt; 500k).</summary>
        [AllowAnonymous]
        [HttpGet("vip-status")]
        public async Task<ActionResult> GetVipStatus(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            phone = (phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var account = await _db.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.Phone == phone, cancellationToken);
            var settings = await _db.ShopSettings.FirstOrDefaultAsync(cancellationToken);

            var totalSpent = account?.TotalSpent ?? 0m;
            var tier = "Đồng";
            if (settings != null)
            {
                if (settings.TierGoldMin > 0 && totalSpent > settings.TierGoldMin)
                {
                    tier = "Vàng";
                }
                else if (settings.TierSilverMin > 0 && totalSpent >= settings.TierSilverMin)
                {
                    tier = "Bạc";
                }
            }

            var isVip = tier == "Vàng" || totalSpent > 500000m;
            return Ok(new
            {
                tier,
                isVip,
                totalSpent,
                zaloGroupUrl = isVip ? settings?.ZaloVipGroupUrl : null
            });
        }

        // ============ Bán sỉ / xuất khẩu (item 58) ============

        /// <summary>Public: thông tin liên hệ bán sỉ.</summary>
        [AllowAnonymous]
        [HttpGet("wholesale-info")]
        public async Task<ActionResult> GetWholesaleInfo(CancellationToken cancellationToken = default)
        {
            var settings = await _db.ShopSettings.FirstOrDefaultAsync(cancellationToken);
            return Ok(new
            {
                contact = settings?.WholesaleContact,
                minQty = settings?.WholesaleMinQty ?? 50
            });
        }

        // ============ Khóa học (item 60) ============

        /// <summary>Public: danh sách khóa học đang mở.</summary>
        [AllowAnonymous]
        [HttpGet("courses")]
        public async Task<ActionResult> GetCourses(CancellationToken cancellationToken = default)
        {
            var courses = await _db.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Title)
                .Select(c => new
                {
                    id = c.Id,
                    title = c.Title,
                    description = c.Description,
                    price = c.Price,
                    thumbnailUrl = c.ThumbnailUrl
                })
                .ToListAsync(cancellationToken);

            return Ok(courses);
        }

        public class CourseEnrollRequest
        {
            public string Phone { get; set; }
        }

        /// <summary>Public: đăng ký khóa học bằng SĐT.</summary>
        [AllowAnonymous]
        [HttpPost("courses/{id:guid}/enroll")]
        public async Task<ActionResult> EnrollCourse(
            Guid id,
            [FromBody] CourseEnrollRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var course = await _db.Courses
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);
            if (course == null)
            {
                return NotFound(new { success = false, message = "Khóa học không tồn tại." });
            }

            var exists = await _db.CourseEnrollments
                .AnyAsync(e => e.CourseId == id && e.Phone == phone, cancellationToken);
            if (exists)
            {
                return Ok(new { success = true, message = "Bạn đã đăng ký khóa học này rồi." });
            }

            _db.CourseEnrollments.Add(new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                CourseId = id,
                Phone = phone,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Đăng ký thành công! Quán sẽ liên hệ hướng dẫn thanh toán." });
        }

        // ============ Retention: off-peak promo (item 48) ============

        /// <summary>Public: active off-peak promos and whether now is in the window.</summary>
        [AllowAnonymous]
        [HttpGet("offpeak")]
        public async Task<ActionResult> GetOffPeak(CancellationToken cancellationToken = default)
        {
            var nowHour = DateTime.Now.Hour;
            var promos = await _db.OffPeakPromos
                .Where(p => p.IsActive)
                .OrderBy(p => p.StartHour)
                .Select(p => new
                {
                    id = p.Id,
                    startHour = p.StartHour,
                    endHour = p.EndHour,
                    discountAmount = p.DiscountAmount,
                    isNow = nowHour >= p.StartHour && nowHour < p.EndHour
                })
                .ToListAsync(cancellationToken);

            return Ok(promos);
        }

        // ============ Retention: 7-day check-in challenge (item 53) ============

        public class CheckInRequest
        {
            public string Phone { get; set; }
        }

        /// <summary>
        /// Public: daily check-in. Returns consecutive-day streak ending today
        /// and whether the 7-day challenge is completed.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("checkin")]
        public async Task<ActionResult> CheckIn(
            [FromBody] CheckInRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();

            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var today = DateTime.UtcNow.Date;
            var exists = await _db.CheckInStreaks
                .AnyAsync(c => c.Phone == phone && c.CheckInDate == today, cancellationToken);

            if (!exists)
            {
                _db.CheckInStreaks.Add(new CheckInStreak
                {
                    Id = Guid.NewGuid(),
                    Phone = phone,
                    CheckInDate = today,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            // Count consecutive days ending today.
            var dates = await _db.CheckInStreaks
                .Where(c => c.Phone == phone && c.CheckInDate <= today)
                .Select(c => c.CheckInDate)
                .Distinct()
                .OrderByDescending(d => d)
                .Take(30)
                .ToListAsync(cancellationToken);

            var streak = 0;
            var cursor = today;
            foreach (var d in dates)
            {
                if (d == cursor) { streak++; cursor = cursor.AddDays(-1); }
                else break;
            }

            return Ok(new { success = true, streakCount = streak, isCompleted = streak >= 7 });
        }

        // ============ Retention: dish polls (item 54) ============

        /// <summary>Public: active dish polls for voting.</summary>
        [AllowAnonymous]
        [HttpGet("polls")]
        public async Task<ActionResult> GetPolls(CancellationToken cancellationToken = default)
        {
            var polls = await _db.DishPolls
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.VoteCount)
                .ThenByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    id = p.Id,
                    dishName = p.DishName,
                    description = p.Description,
                    imageUrl = p.ImageUrl,
                    voteCount = p.VoteCount
                })
                .ToListAsync(cancellationToken);

            return Ok(polls);
        }

        public class VoteRequest
        {
            public string Phone { get; set; }
        }

        /// <summary>Public: vote for a dish poll (one vote per phone per poll).</summary>
        [AllowAnonymous]
        [HttpPost("polls/{id:guid}/vote")]
        public async Task<ActionResult> VotePoll(
            Guid id,
            [FromBody] VoteRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();

            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var poll = await _db.DishPolls.FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
            if (poll == null)
            {
                return NotFound(new { success = false, message = "Bình chọn không tồn tại." });
            }

            var alreadyVoted = await _db.DishVotes
                .AnyAsync(v => v.PollId == id && v.Phone == phone, cancellationToken);

            if (alreadyVoted)
            {
                return Ok(new { success = false, message = "Bạn đã bình chọn món này rồi." });
            }

            _db.DishVotes.Add(new DishVote
            {
                Id = Guid.NewGuid(),
                PollId = id,
                Phone = phone,
                CreatedAt = DateTime.UtcNow
            });
            poll.VoteCount++;
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, voteCount = poll.VoteCount });
        }

        /// <summary>
        // ============ Parse ý định đặt hàng từ text/chatbot/voice (items 61, 80) ============

        public class ParseOrderRequest
        {
            public string Text { get; set; }
            public string Phone { get; set; }
        }

        /// <summary>
        /// Public: parse Vietnamese free text into dish lines.
        /// Does NOT create an order — the frontend confirms before calling the order API.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("parse-order")]
        public async Task<ActionResult> ParseOrder(
            [FromBody] ParseOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            var text = request?.Text ?? string.Empty;
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.Active)
                .ToListAsync(cancellationToken);

            var result = OrderIntentParser.Parse(text, products);

            return Ok(new
            {
                isOrderIntent = result.IsOrderIntent,
                items = result.Items.Select(i => new
                {
                    productId = i.ProductId,
                    productName = i.ProductName,
                    quantity = i.Quantity,
                    price = i.Price
                }),
                unmatchedText = result.UnmatchedText
            });
        }

        // ============ Món yêu thích (item 63) ============

        public class FavoriteRequest
        {
            public string Phone { get; set; }
            public Guid ProductId { get; set; }
        }

        /// <summary>Public: add a favorite dish (idempotent).</summary>
        [AllowAnonymous]
        [HttpPost("favorites")]
        public async Task<ActionResult> AddFavorite(
            [FromBody] FavoriteRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var exists = await _db.FavoriteDishes
                .AnyAsync(f => f.Phone == phone && f.ProductId == request.ProductId, cancellationToken);

            if (!exists)
            {
                _db.FavoriteDishes.Add(new FavoriteDish
                {
                    Phone = phone,
                    ProductId = request.ProductId
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Ok(new { success = true });
        }

        /// <summary>Public: remove a favorite dish.</summary>
        [AllowAnonymous]
        [HttpDelete("favorites")]
        public async Task<ActionResult> RemoveFavorite(
            [FromBody] FavoriteRequest request,
            CancellationToken cancellationToken = default)
        {
            var phone = (request?.Phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var fav = await _db.FavoriteDishes
                .FirstOrDefaultAsync(f => f.Phone == phone && f.ProductId == request.ProductId, cancellationToken);

            if (fav != null)
            {
                _db.FavoriteDishes.Remove(fav);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Ok(new { success = true });
        }

        /// <summary>Public: list favorite product ids for a phone.</summary>
        [AllowAnonymous]
        [HttpGet("favorites")]
        public async Task<ActionResult> GetFavorites(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            phone = (phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var ids = await _db.FavoriteDishes
                .Where(f => f.Phone == phone)
                .Select(f => f.ProductId)
                .ToListAsync(cancellationToken);

            return Ok(new { productIds = ids });
        }

        /// <summary>
        /// Public: favorite dishes that are currently available —
        /// frontend uses this to show "món bạn thích đã có lại".
        /// </summary>
        [AllowAnonymous]
        [HttpGet("favorites/back-in-stock")]
        public async Task<ActionResult> GetFavoritesBackInStock(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            phone = (phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var items = await _db.FavoriteDishes
                .Where(f => f.Phone == phone)
                .Join(_db.Products,
                    f => f.ProductId,
                    p => p.Id,
                    (f, p) => p)
                .Where(p => p.Status == ProductStatus.Active)
                .Select(p => new
                {
                    productId = p.Id,
                    productName = p.Name,
                    price = p.Price.Amount,
                    imageUrlsJson = p.ImageUrlsJson
                })
                .ToListAsync(cancellationToken);

            return Ok(new { items });
        }

        // ============ Link sàn TMĐT (item 75) ============

        /// <summary>Public: marketplace links (only returned when configured).</summary>
        [AllowAnonymous]
        [HttpGet("marketplace-links")]
        public async Task<ActionResult> GetMarketplaceLinks(CancellationToken cancellationToken = default)
        {
            var settings = await _db.ShopSettings.FirstOrDefaultAsync(cancellationToken);

            return Ok(new
            {
                shopeeFoodUrl = string.IsNullOrWhiteSpace(settings?.ShopeeFoodUrl) ? null : settings.ShopeeFoodUrl,
                grabFoodUrl = string.IsNullOrWhiteSpace(settings?.GrabFoodUrl) ? null : settings.GrabFoodUrl
            });
        }

        // ============ Gợi ý đặt lại theo lịch sử (item 62) ============

        /// <summary>
        /// Public: items of the most recent order for a phone number,
        /// so the frontend can offer one-tap reorder. Empty array when no order.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("last-order-items")]
        public async Task<ActionResult> GetLastOrderItems(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            phone = (phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ." });
            }

            var order = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CustomerPhone == phone)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(cancellationToken);

            var items = order == null
                ? new List<object>()
                : order.Lines
                    .Select(l => (object)new
                    {
                        productId = l.ProductId,
                        productName = l.ProductName,
                        quantity = l.Quantity,
                        price = l.UnitPrice.Amount
                    })
                    .ToList();

            return Ok(new { items });
        }

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
