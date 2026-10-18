using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Security;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Security;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Application.AuditLogs;

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
        private readonly ILoginAuditAppService _loginAudits;
        private readonly ITwoFactorAppService _twoFactor;
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;
        private readonly PasswordResetOtpService _passwordReset;

        public AuthController(
            UserManager<User> userManager,
            IUserRepository users,
            JwtTokenService jwtTokenService,
            ILoginAuditAppService loginAudits,
            ITwoFactorAppService twoFactor,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger,
            PasswordResetOtpService passwordReset)
        {
            _userManager = userManager;
            _users = users;
            _jwtTokenService = jwtTokenService;
            _loginAudits = loginAudits;
            _twoFactor = twoFactor;
            _permissions = permissions;
            _activityLogger = activityLogger;
            _passwordReset = passwordReset;
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

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var login = model.UsernameOrEmail.Trim();
            var user = await _userManager.FindByNameAsync(login)
                ?? (login.Contains("@") ? await _userManager.FindByEmailAsync(login) : null);

            if (user == null)
            {
                // Item 99: every login attempt is audited.
                await _loginAudits.RecordAsync(login, ip, false, "Không tìm thấy tài khoản.");
                return Unauthorized("Invalid username/email or password.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _loginAudits.RecordAsync(user.UserName, ip, false, "Tài khoản tạm khóa.");
                // Item 100: Vietnamese lockout message.
                return StatusCode(423, "Tài khoản tạm khóa. Vui lòng thử lại sau.");
            }

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await _userManager.AccessFailedAsync(user);
                await _loginAudits.RecordAsync(user.UserName, ip, false, "Sai mật khẩu.");
                return Unauthorized("Invalid username/email or password.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            // Item 99: warn when the successful login comes from an IP never
            // seen before for this user.
            var newIpWarning = await _loginAudits.IsNewIpForUserAsync(user.UserName, ip);
            await _loginAudits.RecordAsync(user.UserName, ip, true);

            var roles = await GetRoleNamesAsync(user.Id);

            // Item 156: log staff logins in the staff activity log.
            if (roles.Any(r => r == "Admin" || r == "Cashier" || r == "Kitchen"))
            {
                await _activityLogger.LogForUserAsync(user.Id, user.UserName, StaffActions.Login, "Đăng nhập trang quản trị.");
            }

            // Item 98: 2FA — password OK, now require the 6-digit TOTP code.
            if (user.TwoFactorEnabled)
            {
                var twoFactorToken = _jwtTokenService.CreateTwoFactorToken(user);
                return Ok(new LoginResponseModel
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = twoFactorToken.Token,
                    UserName = user.UserName
                });
            }

            var (token, expiresAt) = _jwtTokenService.CreateToken(user, roles);

            return Ok(new LoginResponseModel
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles,
                NewIpWarning = newIpWarning,
                NewIpAddress = newIpWarning ? ip : null
            });
        }

        /// <summary>
        /// Customer self-registration with VN phone + password. No paid OTP
        /// service — phone format is validated, uniqueness is enforced.
        /// Issues a JWT immediately so the customer is logged in at once.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] CustomerRegisterRequestModel model,
            CancellationToken cancellationToken = default)
        {
            if (model == null)
            {
                return BadRequest("Thông tin đăng ký không hợp lệ.");
            }

            var phone = (model.Phone ?? string.Empty).Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(0)(3|5|7|8|9)\d{8}$"))
            {
                return BadRequest("Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
            {
                return BadRequest("Mật khẩu phải có ít nhất 6 ký tự.");
            }

            var existing = await _userManager.FindByNameAsync(phone);
            if (existing != null)
            {
                return Conflict("Số điện thoại này đã được đăng ký. Vui lòng đăng nhập.");
            }

            var name = string.IsNullOrWhiteSpace(model.FullName) ? phone : model.FullName.Trim();
            var user = new User
            {
                UserName = phone,
                NormalizedUserName = phone.ToUpperInvariant(),
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            // Ensure the Customer role exists, then assign it.
            var dbContext = HttpContext.RequestServices
                .GetService(typeof(PhuThuongStickyRice.Persistence.StickyRiceDbContext))
                as PhuThuongStickyRice.Persistence.StickyRiceDbContext;
            if (dbContext != null)
            {
                var roleName = Services.AdminSeedHostedService.CustomerRoleName;
                var role = await dbContext.Set<Role>()
                    .FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), cancellationToken);
                if (role == null)
                {
                    role = new Role
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };
                    await dbContext.Set<Role>().AddAsync(role, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var alreadyInRole = await dbContext.Set<UserRole>()
                    .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
                if (!alreadyInRole)
                {
                    await dbContext.Set<UserRole>().AddAsync(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    }, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _loginAudits.RecordAsync(user.UserName, ip, true, "Đăng ký tài khoản khách hàng.");

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

        /// <summary>
        /// Item 98: completes the 2FA login — validates the short-lived
        /// two-factor token plus the 6-digit TOTP code, then issues the JWT.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("2fa/verify")]
        public async Task<ActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequestModel model,
            CancellationToken cancellationToken = default)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.TwoFactorToken)
                || string.IsNullOrWhiteSpace(model.Code))
            {
                return BadRequest("Two-factor token and code are required.");
            }

            var userId = _jwtTokenService.ValidateTwoFactorToken(model.TwoFactorToken);
            if (!userId.HasValue)
            {
                return Unauthorized("Mã đăng nhập 2 bước đã hết hạn. Vui lòng đăng nhập lại.");
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null)
            {
                return Unauthorized("Invalid two-factor token.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return StatusCode(423, "Tài khoản tạm khóa. Vui lòng thử lại sau.");
            }

            if (!await _twoFactor.VerifyCodeAsync(user.Id, model.Code))
            {
                await _userManager.AccessFailedAsync(user);
                await _loginAudits.RecordAsync(user.UserName, ip, false, "Sai mã 2FA.");
                return Unauthorized("Mã xác thực 2FA không đúng.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var newIpWarning = await _loginAudits.IsNewIpForUserAsync(user.UserName, ip);
            await _loginAudits.RecordAsync(user.UserName, ip, true);

            var roles = await GetRoleNamesAsync(user.Id);

            // Item 156: log staff logins (2FA path) in the staff activity log.
            if (roles.Any(r => r == "Admin" || r == "Cashier" || r == "Kitchen"))
            {
                await _activityLogger.LogForUserAsync(user.Id, user.UserName, StaffActions.Login, "Đăng nhập trang quản trị (2FA).");
            }

            var (token, expiresAt) = _jwtTokenService.CreateToken(user, roles);

            return Ok(new LoginResponseModel
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles,
                NewIpWarning = newIpWarning,
                NewIpAddress = newIpWarning ? ip : null
            });
        }

        /// <summary>Item 98: returns the otpauth:// QR to set up 2FA (Admin).</summary>
        [Authorize(Roles = "Admin")]
        /// <summary>Lấy thông tin tài khoản đang đăng nhập.</summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(new
            {
                user.UserName,
                user.Email,
                user.PhoneNumber,
            });
        }

        /// <summary>Cập nhật thông tin tài khoản đang đăng nhập (tên, email, SĐT).</summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName.Trim() != user.UserName)
            {
                var setName = await _userManager.SetUserNameAsync(user, model.UserName.Trim());
                if (!setName.Succeeded)
                    return BadRequest(string.Join("; ", setName.Errors.Select(e => e.Description)));
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && model.Email.Trim() != user.Email)
            {
                var setEmail = await _userManager.SetEmailAsync(user, model.Email.Trim());
                if (!setEmail.Succeeded)
                    return BadRequest(string.Join("; ", setEmail.Errors.Select(e => e.Description)));
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var phone = model.PhoneNumber.Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(0)(3|5|7|8|9)\d{8}$"))
                    return BadRequest("Số điện thoại không đúng định dạng (VD: 0901234567).");
                var setPhone = await _userManager.SetPhoneNumberAsync(user, phone);
                if (!setPhone.Succeeded)
                    return BadRequest(string.Join("; ", setPhone.Errors.Select(e => e.Description)));
            }

            return Ok(new { message = "Đã cập nhật." });
        }

        [HttpGet("2fa/setup")]
        public async Task<ActionResult> GetTwoFactorSetup(CancellationToken cancellationToken = default)
        {
            return Ok(await _twoFactor.GetSetupAsync());
        }

        /// <summary>Item 98: verifies the code and enables 2FA (Admin).</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("2fa/enable")]
        public async Task<ActionResult> EnableTwoFactor([FromBody] EnableTwoFactorRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _twoFactor.EnableAsync(new EnableTwoFactorDto
                {
                    Secret = model?.Secret,
                    Code = model?.Code
                });
                return Ok(new { enabled = result.IsEnabled });
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Item 98: disables 2FA (Admin, code required).</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("2fa/disable")]
        public async Task<ActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _twoFactor.DisableAsync(new VerifyTwoFactorDto { Code = model?.Code });
                return Ok(new { enabled = false });
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

        /// <summary>
        /// Customer forgot-password: requests a 6-digit OTP via SMS.
        /// Always returns success — it never reveals whether the phone
        /// number is registered (anti-enumeration).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("forgot-password/request")]
        public async Task<ActionResult> ForgotPasswordRequest(
            [FromBody] ForgotPasswordRequestModel model,
            CancellationToken cancellationToken = default)
        {
            await _passwordReset.RequestOtpAsync(model?.Phone, cancellationToken);
            return Ok(new { message = "Nếu số điện thoại đã được đăng ký, mã OTP sẽ được gửi qua SMS trong ít phút." });
        }

        /// <summary>
        /// Customer forgot-password: verifies the OTP and sets a new password.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("forgot-password/reset")]
        public async Task<ActionResult> ForgotPasswordReset(
            [FromBody] ForgotPasswordResetModel model,
            CancellationToken cancellationToken = default)
        {
            var result = await _passwordReset.ResetPasswordAsync(
                model?.Phone, model?.Otp, model?.NewPassword, cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
        }

        /// <summary>
        /// Item 155: fine-grained permission keys of the current user.
        /// The admin frontend caches this to hide/disable sensitive buttons;
        /// every sensitive API still re-checks server-side.
        /// </summary>
        [Authorize]
        [HttpGet("my-permissions")]
        public async Task<ActionResult> GetMyPermissions(CancellationToken cancellationToken = default)
        {
            var permissions = await _permissions.GetUserPermissionsAsync();
            return Ok(new { permissions = permissions.OrderBy(p => p).ToList() });
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

    public sealed class CustomerRegisterRequestModel
    {
        [Required]
        public string Phone { get; set; }

        [Required]
        public string Password { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }
    }

    public sealed class ChangePasswordRequestModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }

    public sealed class ForgotPasswordRequestModel
    {
        [Required]
        public string Phone { get; set; }
    }

    public sealed class ForgotPasswordResetModel
    {
        [Required]
        public string Phone { get; set; }

        [Required]
        public string Otp { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }

    public sealed class VerifyTwoFactorRequestModel
    {
        [Required]
        public string TwoFactorToken { get; set; }

        [Required]
        public string Code { get; set; }
    }

    public sealed class EnableTwoFactorRequestModel
    {
        [Required]
        public string Secret { get; set; }

        [Required]
        public string Code { get; set; }
    }

    public sealed class DisableTwoFactorRequestModel
    {
        [Required]
        public string Code { get; set; }
    }

    public sealed class LoginResponseModel
    {
        public string AccessToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string[] Roles { get; set; }

        /// <summary>Item 98: true when a TOTP code is still required.</summary>
        public bool RequiresTwoFactor { get; set; }

        /// <summary>Item 98: short-lived token for the 2FA verification step.</summary>
        public string TwoFactorToken { get; set; }

        /// <summary>Item 99: true when this login came from a never-seen IP.</summary>
        public bool NewIpWarning { get; set; }

        public string NewIpAddress { get; set; }
    }

    public class UpdateProfileModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
