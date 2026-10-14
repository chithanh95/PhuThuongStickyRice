using Microsoft.EntityFrameworkCore;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// In-repo admin authentication. Issues self-signed JWTs — no external
    /// IdentityServer is involved.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _users;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(
            UserManager<User> userManager,
            IUserRepository users,
            JwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _users = users;
            _jwtTokenService = jwtTokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequestModel model,
            CancellationToken cancellationToken = default)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.UsernameOrEmail)
                || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest("Username/email and password are required.");
            }

            var login = model.UsernameOrEmail.Trim();
            var user = await _userManager.FindByNameAsync(login)
                ?? (login.Contains("@") ? await _userManager.FindByEmailAsync(login) : null);

            if (user == null)
            {
                return Unauthorized("Invalid username/email or password.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return StatusCode(423, "Account is temporarily locked. Please try again later.");
            }

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await _userManager.AccessFailedAsync(user);
                return Unauthorized("Invalid username/email or password.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await GetRoleNamesAsync(user.Id);
            var (token, expiresAt) = _jwtTokenService.CreateToken(user, roles);

            return Ok(new LoginResponseModel
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestModel model,
            CancellationToken cancellationToken = default)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CurrentPassword)
                || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest("Current password and new password are required.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            return Ok(new { message = "Password changed." });
        }

        private async Task<string[]> GetRoleNamesAsync(Guid userId)
        {
            // The template UserStore does not implement IUserRoleStore, so
            // roles are read directly from the repository (UserRoles + Role).
            var user = await _users.Get(new UserQueryOptions { IncludeRoles = true })
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user?.UserRoles == null)
            {
                return Array.Empty<string>();
            }

            return user.UserRoles
                .Select(x => x.Role?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public sealed class LoginRequestModel
    {
        [Required]
        public string UsernameOrEmail { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public sealed class ChangePasswordRequestModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }

    public sealed class LoginResponseModel
    {
        public string AccessToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string[] Roles { get; set; }
    }
}
