using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Admin CRUD for retention features: off-peak promos (item 48)
    /// and dish polls (item 54).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/admin/retention")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RetentionAdminController : ControllerBase
    {
        private readonly StickyRiceDbContext _db;
        private readonly IMemoryCache _memoryCache;

        public RetentionAdminController(StickyRiceDbContext db, IMemoryCache memoryCache)
        {
            _db = db;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Perf: busts all hourly off-peak promo cache entries so an admin
        /// edit shows up immediately on the public landing page.
        /// </summary>
        private void InvalidateOffPeakCache()
        {
            for (var hour = 0; hour < 24; hour++)
            {
                _memoryCache.Remove(LandingPublicController.LandingOffPeakCacheKeyPrefix + hour);
            }
        }

        // ============ Off-peak promos ============

        [HttpGet("offpeak")]
        public async Task<ActionResult> GetOffPeakPromos(CancellationToken cancellationToken = default)
        {
            var promos = await _db.OffPeakPromos
                .OrderBy(p => p.StartHour)
                .Select(p => new
                {
                    id = p.Id,
                    startHour = p.StartHour,
                    endHour = p.EndHour,
                    discountAmount = p.DiscountAmount,
                    isActive = p.IsActive
                })
                .ToListAsync(cancellationToken);

            return Ok(promos);
        }

        public class OffPeakPromoDto
        {
            public int StartHour { get; set; }
            public int EndHour { get; set; }
            public int DiscountAmount { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpPost("offpeak")]
        public async Task<ActionResult> CreateOffPeakPromo(
            [FromBody] OffPeakPromoDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto.StartHour < 0 || dto.StartHour > 23 || dto.EndHour < 0 || dto.EndHour > 23
                || dto.StartHour == dto.EndHour || dto.DiscountAmount < 0)
            {
                return BadRequest(new { success = false, message = "Khung giờ hoặc mức giảm không hợp lệ." });
            }

            var promo = new OffPeakPromo
            {
                Id = Guid.NewGuid(),
                StartHour = dto.StartHour,
                EndHour = dto.EndHour,
                DiscountAmount = dto.DiscountAmount,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _db.OffPeakPromos.Add(promo);
            await _db.SaveChangesAsync(cancellationToken);
            InvalidateOffPeakCache();

            return Ok(new { success = true, id = promo.Id });
        }

        [HttpPut("offpeak/{id:guid}")]
        public async Task<ActionResult> UpdateOffPeakPromo(
            Guid id,
            [FromBody] OffPeakPromoDto dto,
            CancellationToken cancellationToken = default)
        {
            var promo = await _db.OffPeakPromos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (promo == null) return NotFound();

            if (dto.StartHour < 0 || dto.StartHour > 23 || dto.EndHour < 0 || dto.EndHour > 23
                || dto.StartHour == dto.EndHour || dto.DiscountAmount < 0)
            {
                return BadRequest(new { success = false, message = "Khung giờ hoặc mức giảm không hợp lệ." });
            }

            promo.StartHour = dto.StartHour;
            promo.EndHour = dto.EndHour;
            promo.DiscountAmount = dto.DiscountAmount;
            promo.IsActive = dto.IsActive;
            await _db.SaveChangesAsync(cancellationToken);
            InvalidateOffPeakCache();

            return Ok(new { success = true });
        }

        [HttpDelete("offpeak/{id:guid}")]
        public async Task<ActionResult> DeleteOffPeakPromo(Guid id, CancellationToken cancellationToken = default)
        {
            var promo = await _db.OffPeakPromos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (promo == null) return NotFound();

            _db.OffPeakPromos.Remove(promo);
            await _db.SaveChangesAsync(cancellationToken);
            InvalidateOffPeakCache();

            return Ok(new { success = true });
        }

        // ============ Dish polls ============

        [HttpGet("polls")]
        public async Task<ActionResult> GetPolls(CancellationToken cancellationToken = default)
        {
            var polls = await _db.DishPolls
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    id = p.Id,
                    dishName = p.DishName,
                    description = p.Description,
                    imageUrl = p.ImageUrl,
                    voteCount = p.VoteCount,
                    isActive = p.IsActive
                })
                .ToListAsync(cancellationToken);

            return Ok(polls);
        }

        public class DishPollDto
        {
            public string DishName { get; set; }
            public string Description { get; set; }
            public string ImageUrl { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpPost("polls")]
        public async Task<ActionResult> CreatePoll(
            [FromBody] DishPollDto dto,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.DishName))
            {
                return BadRequest(new { success = false, message = "Tên món không được trống." });
            }

            var poll = new DishPoll
            {
                Id = Guid.NewGuid(),
                DishName = dto.DishName.Trim(),
                Description = dto.Description?.Trim(),
                ImageUrl = dto.ImageUrl?.Trim(),
                VoteCount = 0,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _db.DishPolls.Add(poll);
            await _db.SaveChangesAsync(cancellationToken);
            _memoryCache.Remove(LandingPublicController.LandingPollsCacheKey);

            return Ok(new { success = true, id = poll.Id });
        }

        [HttpPut("polls/{id:guid}")]
        public async Task<ActionResult> UpdatePoll(
            Guid id,
            [FromBody] DishPollDto dto,
            CancellationToken cancellationToken = default)
        {
            var poll = await _db.DishPolls.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (poll == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.DishName))
            {
                return BadRequest(new { success = false, message = "Tên món không được trống." });
            }

            poll.DishName = dto.DishName.Trim();
            poll.Description = dto.Description?.Trim();
            poll.ImageUrl = dto.ImageUrl?.Trim();
            poll.IsActive = dto.IsActive;
            await _db.SaveChangesAsync(cancellationToken);
            _memoryCache.Remove(LandingPublicController.LandingPollsCacheKey);

            return Ok(new { success = true });
        }

        [HttpDelete("polls/{id:guid}")]
        public async Task<ActionResult> DeletePoll(Guid id, CancellationToken cancellationToken = default)
        {
            var poll = await _db.DishPolls.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (poll == null) return NotFound();

            var votes = _db.DishVotes.Where(v => v.PollId == id);
            _db.DishVotes.RemoveRange(votes);
            _db.DishPolls.Remove(poll);
            await _db.SaveChangesAsync(cancellationToken);
            _memoryCache.Remove(LandingPublicController.LandingPollsCacheKey);

            return Ok(new { success = true });
        }
    }
}
