using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Staff account management (Admin only): list users, create cashier/
    /// kitchen accounts, assign/remove roles. Roles are the seeded ones:
    /// Admin, Cashier, Kitchen.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private static readonly string[] AllowedRoles = { "Admin", "Cashier", "Kitchen" };

        private readonly UserManager<User> _userManager;
        private readonly StickyRiceDbContext _db;
        private readonly IStaffActivityLogger _activityLogger;

        public StaffController(
            UserManager<User> userManager,
            StickyRiceDbContext db,
            IStaffActivityLogger activityLogger)
        {
            _userManager = userManager;
            _db = db;
            _activityLogger = activityLogger;
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetUsers(CancellationToken cancellationToken = default)
        {
            // perf-hunt 2026-10-19: chỉ lấy đúng 6 cột cần trong SQL + join role
            // trong SQL. Trước đây nạp full entity User (kéo theo PasswordHash,
            // SecurityStamp...) và toàn bộ 2 bảng về memory rồi join tay O(n*m).
            var users = await _db.Set<User>().OrderBy(u => u.UserName)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.PhoneNumber,
                    u.LockoutEnd
                })
                .ToListAsync(cancellationToken);
            var roleNames = await (from ur in _db.Set<UserRole>()
                                   join r in _db.Set<Role>() on ur.RoleId equals r.Id
                                   select new { ur.UserId, r.Name })
                .ToListAsync(cancellationToken);
            var rolesByUser = roleNames
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Name).OrderBy(n => n).ToList());

            var result = users.Select(u => new
            {
                id = u.Id,
                userName = u.UserName,
                email = u.Email,
                phoneNumber = u.PhoneNumber,
                isLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                roles = rolesByUser.TryGetValue(u.Id, out var names)
                    ? names
                    : new List<string>()
            });

            return Ok(result);
        }

        public class CreateStaffRequest
        {
            [Required, MaxLength(256)]
            public string UserName { get; set; }

            [EmailAddress, MaxLength(256)]
            public string Email { get; set; }

            [Required, MinLength(6)]
            public string Password { get; set; }

            [Required]
            public string Role { get; set; }
        }

        [HttpPost("users")]
        public async Task<ActionResult> CreateUser(
            [FromBody] CreateStaffRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!AllowedRoles.Contains(request.Role))
                return BadRequest(new { message = "Role không hợp lệ." });

            var existing = await _userManager.FindByNameAsync(request.UserName);
            if (existing != null)
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại." });

            var user = new User
            {
                UserName = request.UserName,
                Email = string.IsNullOrWhiteSpace(request.Email) ? request.UserName : request.Email,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });

            await AssignRoleAsync(user.Id, request.Role, cancellationToken);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.StaffCreate,
                $"Tạo tài khoản {user.UserName} vai trò {request.Role}.");

            return Ok(new { id = user.Id });
        }

        public class AssignRoleRequest
        {
            [Required]
            public string Role { get; set; }
        }

        [HttpPost("users/{id:guid}/roles")]
        public async Task<ActionResult> AssignRole(
            Guid id,
            [FromBody] AssignRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!AllowedRoles.Contains(request.Role))
                return BadRequest(new { message = "Role không hợp lệ." });

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            await AssignRoleAsync(user.Id, request.Role, cancellationToken);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.StaffRoleChange,
                $"Gán vai trò {request.Role} cho {user.UserName}.");

            return Ok();
        }

        [HttpDelete("users/{id:guid}/roles/{role}")]
        public async Task<ActionResult> RemoveRole(
            Guid id,
            string role,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            // Safety: cannot strip your own Admin role.
            var selfId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                && string.Equals(selfId, id.ToString(), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Không thể tự gỡ quyền Admin của chính mình." });

            var roleEntity = await _db.Set<Role>()
                .FirstOrDefaultAsync(r => r.NormalizedName == role.ToUpperInvariant(), cancellationToken);
            if (roleEntity == null) return NotFound();

            var link = await _db.Set<UserRole>()
                .FirstOrDefaultAsync(x => x.UserId == id && x.RoleId == roleEntity.Id, cancellationToken);
            if (link != null)
            {
                _db.Set<UserRole>().Remove(link);
                await _db.SaveChangesAsync(cancellationToken);

                // Item 156: staff activity log.
                await _activityLogger.LogAsync(StaffActions.StaffRoleChange,
                    $"Gỡ vai trò {role} khỏi {user.UserName}.");
            }

            return Ok();
        }

        private async Task AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken)
        {
            var roleEntity = await _db.Set<Role>()
                .FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), cancellationToken);
            if (roleEntity == null) throw new InvalidOperationException($"Role '{roleName}' chưa được seed.");

            var exists = await _db.Set<UserRole>()
                .AnyAsync(x => x.UserId == userId && x.RoleId == roleEntity.Id, cancellationToken);
            if (!exists)
            {
                await _db.Set<UserRole>().AddAsync(
                    new UserRole { UserId = userId, RoleId = roleEntity.Id }, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
