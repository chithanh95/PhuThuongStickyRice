// Bật nullable annotation context ở cấp file: project chưa bật <Nullable>enable</Nullable>
// toàn cục nên compiler sẽ bỏ qua các annotation string?/...? trong file (CS8632)
// nếu thiếu dòng này. Convention theo RateLimiterPolicies/DefaultRateLimiterPolicy.cs.
// (Bật nullable có thể lộ thêm warning nullable tiềm ẩn trong file — chỉ là warning,
// chấp nhận được.)
#nullable enable
using PhuThuongStickyRice.Application.AuditLogs;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Security;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;

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
        private readonly ICustomerProfileAppService _profiles;
        private readonly ITwoFactorAppService _twoFactor;
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;
        private readonly PasswordResetOtpService _passwordReset;

        private const int MinPasswordLength = 6;
        // Thông báo lỗi định dạng SĐT dùng chung ở Register + UpdateProfile
        // (trước đây lặp lại y nguyên 2 nơi — đổi nội dung chỉ cần 1 chỗ).
        private const string InvalidVnPhoneMessage = "Số điện thoại không đúng định dạng (VD: 0901234567).";
        private const int AccountLockedStatusCode = 423;
        private static readonly string[] StaffRoleNames = { "Admin", "Cashier", "Kitchen" };

        private static bool IsStaffRole(string roleName) =>
            StaffRoleNames.Contains(roleName, StringComparer.Ordinal);

        /// <summary>
        /// Khởi tạo controller xác thực: đăng nhập/đăng ký, 2FA, quên mật khẩu (OTP),
        /// đổi mật khẩu và cập nhật hồ sơ tài khoản.
        /// </summary>
        public AuthController(
            UserManager<User> userManager,
            IUserRepository users,
            JwtTokenService jwtTokenService,
            ILoginAuditAppService loginAudits,
            ICustomerProfileAppService profiles,
            ITwoFactorAppService twoFactor,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger,
            PasswordResetOtpService passwordReset)
        {
            _userManager = userManager;
            _users = users;
            _jwtTokenService = jwtTokenService;
            _loginAudits = loginAudits;
            _profiles = profiles;
            _twoFactor = twoFactor;
            _permissions = permissions;
            _activityLogger = activityLogger;
            _passwordReset = passwordReset;
        }

        /// <summary>
        /// Đăng nhập bằng username/email + mật khẩu: kiểm tra tài khoản bị
        /// khóa, xác thực mật khẩu, ghi audit mọi lần thử (Item 99), cảnh báo
        /// IP mới, log hoạt động của staff (Item 156). Nếu tài khoản bật 2FA
        /// thì trả về thử thách TOTP thay vì JWT (Item 98).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequestModel model,
            CancellationToken cancellationToken = default)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.UsernameOrEmail)
                || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Vui lòng nhập tên đăng nhập và mật khẩu." });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var login = model.UsernameOrEmail.Trim();
            var user = await _userManager.FindByNameAsync(login)
                ?? (login.Contains('@') ? await _userManager.FindByEmailAsync(login) : null);

            if (user == null)
            {
                // Item 99: every login attempt is audited.
                await _loginAudits.RecordAsync(login, clientIp, false, "Không tìm thấy tài khoản.");
                return Unauthorized(new { message = "Thông tin đăng nhập không đúng. Vui lòng kiểm tra lại rồi thử lại." });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _loginAudits.RecordAsync(user.UserName, clientIp, false, "Tài khoản tạm khóa.");
                // Item 100: Vietnamese lockout message.
                return StatusCode(AccountLockedStatusCode, new { message = "Tài khoản tạm khóa. Vui lòng thử lại sau." });
            }

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await _userManager.AccessFailedAsync(user);
                await _loginAudits.RecordAsync(user.UserName, clientIp, false, "Sai mật khẩu.");
                return Unauthorized(new { message = "Thông tin đăng nhập không đúng. Vui lòng kiểm tra lại rồi thử lại." });
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var (roles, newIpWarning) = await AuditSuccessfulLoginAsync(
                user, clientIp, "Đăng nhập trang quản trị.");

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

            return BuildLoginResponse(user, roles, clientIp, newIpWarning);
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
                return BadRequest(new { message = "Thông tin đăng ký không hợp lệ." });
            }

            var phone = (model.Phone ?? string.Empty).Trim();
            if (!PhoneNumber.IsValidVn(phone))
            {
                return BadRequest(new { message = InvalidVnPhoneMessage });
            }

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < MinPasswordLength)
            {
                return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự." });
            }

            var existing = await _userManager.FindByNameAsync(phone);
            if (existing != null)
            {
                return Conflict(new { message = "Số điện thoại này đã được đăng ký. Vui lòng đăng nhập." });
            }

            // [codequality-smell 2026-10-19] Dựng entity User tách ra method
            // riêng để Register ngắn gọn, chỉ còn flow validate → tạo → cấp token.
            var user = BuildCustomerUser(phone, model);

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }

            await EnsureCustomerRoleAsync(user.Id, cancellationToken);

            // Lưu họ tên khách nhập ở form đăng ký. Trước đây trường này bị
            // bỏ mất hoàn toàn (User không có cột tên, BuildCustomerUser
            // không dùng model.FullName) nên checkout không có tên để tự
            // điền cho khách đã đăng nhập (user báo 2026-10-23). Tên lưu
            // dạng claim — fail-soft: lỗi lưu tên KHÔNG được làm hỏng đăng ký.
            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                try
                {
                    await _profiles.SetFullNameAsync(user.Id, model.FullName.Trim());
                }
                catch (Exception)
                {
                    // Tên chỉ là dữ liệu phụ cho tự điền form — bỏ qua khi lỗi.
                }
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            await _loginAudits.RecordAsync(user.UserName, clientIp, true, "Đăng ký tài khoản khách hàng.");

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
        /// Dựng entity <see cref="User"/> cho khách tự đăng ký: username là SĐT
        /// (đã validate), SĐT coi như đã xác thực vì không có SMS OTP thật.
        /// Họ tên (model.FullName) không nằm trên entity này — Register lưu
        /// riêng dạng claim qua ICustomerProfileAppService.SetFullNameAsync.
        /// </summary>
        private static User BuildCustomerUser(string phone, CustomerRegisterRequestModel model)
        {
            return new User
            {
                UserName = phone,
                NormalizedUserName = phone.ToUpperInvariant(),
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
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
                return BadRequest(new { message = "Vui lòng nhập mã xác thực 2 bước." });
            }

            var userId = _jwtTokenService.ValidateTwoFactorToken(model.TwoFactorToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Mã đăng nhập 2 bước đã hết hạn. Vui lòng đăng nhập lại." });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null)
            {
                return Unauthorized(new { message = "Mã đăng nhập 2 bước không hợp lệ. Vui lòng đăng nhập lại." });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return StatusCode(AccountLockedStatusCode, new { message = "Tài khoản tạm khóa. Vui lòng thử lại sau." });
            }

            if (!await _twoFactor.VerifyCodeAsync(user.Id, model.Code))
            {
                await _userManager.AccessFailedAsync(user);
                await _loginAudits.RecordAsync(user.UserName, clientIp, false, "Sai mã 2FA.");
                return Unauthorized(new { message = "Mã xác thực 2FA không đúng." });
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var (roles, newIpWarning) = await AuditSuccessfulLoginAsync(
                user, clientIp, "Đăng nhập trang quản trị (2FA).");

            return BuildLoginResponse(user, roles, clientIp, newIpWarning);
        }

        /// <summary>Lấy thông tin tài khoản đang đăng nhập.</summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Best practice: FindFirstValue trả về string? — guard trước khi
            // truyền vào FindByIdAsync (tránh CS8604 và 500 khi claim thiếu).
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

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
            // Best practice: FindFirstValue trả về string? — guard trước khi
            // truyền vào FindByIdAsync (tránh CS8604 và 500 khi claim thiếu).
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName.Trim() != user.UserName)
            {
                var setName = await _userManager.SetUserNameAsync(user, model.UserName.Trim());
                if (!setName.Succeeded)
                    return BadRequest(new { message = string.Join("; ", setName.Errors.Select(e => e.Description)) });
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && model.Email.Trim() != user.Email)
            {
                var setEmail = await _userManager.SetEmailAsync(user, model.Email.Trim());
                if (!setEmail.Succeeded)
                    return BadRequest(new { message = string.Join("; ", setEmail.Errors.Select(e => e.Description)) });
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var phone = model.PhoneNumber.Trim();
                if (!PhoneNumber.IsValidVn(phone))
                    return BadRequest(new { message = InvalidVnPhoneMessage });
                var setPhone = await _userManager.SetPhoneNumberAsync(user, phone);
                if (!setPhone.Succeeded)
                    return BadRequest(new { message = string.Join("; ", setPhone.Errors.Select(e => e.Description)) });
            }

            return Ok(new { message = "Đã cập nhật." });
        }

        /// <summary>Item 98: returns the otpauth:// QR to set up 2FA (Admin).</summary>
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
                var result = await _twoFactor.EnableAsync(new VerifyTwoFactorDto
                {
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

        /// <summary>
        /// Đổi mật khẩu cho tài khoản đang đăng nhập. Yêu cầu nhập đúng mật khẩu hiện tại;
        /// mật khẩu mới phải đủ độ dài tối thiểu (MinPasswordLength).
        /// </summary>
        /// <param name="model">Mật khẩu hiện tại và mật khẩu mới.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        /// <returns>200 khi đổi thành công; 400/401 khi dữ liệu sai hoặc chưa đăng nhập.</returns>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestModel model,
            CancellationToken cancellationToken = default)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CurrentPassword)
                || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new { message = "Vui lòng nhập mật khẩu hiện tại và mật khẩu mới." });
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
                return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }

            return Ok(new { message = "Đổi mật khẩu thành công." });
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
                return BadRequest(new { message = result.ErrorMessage });
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

        /// <summary>
        /// Ghi audit cho một lần đăng nhập thành công: kiểm tra IP mới
        /// (Item 99), ghi log đăng nhập, log hoạt động của staff (Item 156).
        /// Trả về danh sách role và cờ cảnh báo IP mới để dựng response.
        /// Dùng chung cho Login thường và VerifyTwoFactor.
        /// </summary>
        private async Task<(string[] Roles, bool NewIpWarning)> AuditSuccessfulLoginAsync(
            User user, string clientIp, string staffLoginNote)
        {
            // Item 99: cảnh báo khi đăng nhập thành công từ IP chưa từng thấy của user.
            var newIpWarning = await _loginAudits.IsNewIpForUserAsync(user.UserName, clientIp);
            await _loginAudits.RecordAsync(user.UserName, clientIp, true);

            var roles = await GetRoleNamesAsync(user.Id);

            // Item 156: log staff logins trong staff activity log.
            if (roles.Any(IsStaffRole))
            {
                await _activityLogger.LogForUserAsync(user.Id, user.UserName, StaffActivityActions.Login, staffLoginNote);
            }

            return (roles, newIpWarning);
        }

        /// <summary>
        /// Dựng response đăng nhập thành công: cấp JWT và đóng gói
        /// <see cref="LoginResponseModel"/>. Dùng chung cho Login thường và
        /// VerifyTwoFactor (sau khi 2FA đã xác thực xong).
        /// </summary>
        // CA1859: phương thức private này chỉ trả về Ok(...) nên dùng kiểu cụ thể
        // OkObjectResult thay vì ActionResult (tránh chuyển đổi kiểu lúc chạy).
        private OkObjectResult BuildLoginResponse(User user, string[] roles, string? clientIp, bool newIpWarning)
        {
            var (token, expiresAt) = _jwtTokenService.CreateToken(user, roles);

            return Ok(new LoginResponseModel
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles,
                NewIpWarning = newIpWarning,
                NewIpAddress = newIpWarning ? clientIp : null
            });
        }

        /// <summary>
        /// Ensures the Customer role exists and the new customer account is
        /// assigned to it (extracted from Register).
        /// </summary>
        private async Task EnsureCustomerRoleAsync(Guid userId, CancellationToken cancellationToken)
        {
            var dbContext = HttpContext.RequestServices
                .GetService(typeof(PhuThuongStickyRice.Persistence.StickyRiceDbContext))
                as PhuThuongStickyRice.Persistence.StickyRiceDbContext;
            if (dbContext == null)
            {
                return;
            }

            var roleName = Services.AdminSeedHostedService.CustomerRoleName;
            // CA1862: chuẩn hóa chuỗi so sánh TRƯỚC khi đưa vào biểu thức
            // LINQ-to-EF. Gợi ý string.Equals(..., StringComparison) của
            // analyzer không dịch được sang SQL (sẽ nổ runtime), còn giữ
            // ToUpperInvariant() trong lambda vẫn bị báo — tính trước ra
            // biến, EF nhận đúng 1 tham số, hành vi truy vấn giữ nguyên.
            var normalizedRoleName = roleName.ToUpperInvariant();
            var role = await dbContext.Set<Role>()
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
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
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken);
            if (!alreadyInRole)
            {
                await dbContext.Set<UserRole>().AddAsync(new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id
                }, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
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
                // Where ở trên đã loại hết null/whitespace nên ép non-null an toàn (tránh CS8619).
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <summary>
    /// Body request đăng nhập: chấp nhận username hoặc email cùng mật khẩu.
    /// </summary>
    public sealed class LoginRequestModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public required string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public required string Password { get; set; }
    }

    /// <summary>
    /// Body request đăng ký tài khoản khách hàng bằng số điện thoại.
    /// </summary>
    public sealed class CustomerRegisterRequestModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public required string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public required string Password { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }
    }

    /// <summary>
    /// Body request đổi mật khẩu (yêu cầu mật khẩu hiện tại).
    /// </summary>
    public sealed class ChangePasswordRequestModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        public required string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        public required string NewPassword { get; set; }
    }

    /// <summary>
    /// Body request bước 1 quên mật khẩu: gửi mã OTP 6 số về SĐT.
    /// </summary>
    public sealed class ForgotPasswordRequestModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public required string Phone { get; set; }
    }

    /// <summary>
    /// Body request bước 2 quên mật khẩu: xác thực OTP và đặt mật khẩu mới.
    /// </summary>
    public sealed class ForgotPasswordResetModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public required string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
        public required string Otp { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        public required string NewPassword { get; set; }
    }

    /// <summary>
    /// Body request xác thực mã 2FA (TOTP) khi đăng nhập.
    /// </summary>
    public sealed class VerifyTwoFactorRequestModel
    {
        [Required(ErrorMessage = "Thiếu mã đăng nhập 2 bước. Vui lòng đăng nhập lại.")]
        public required string TwoFactorToken { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã xác thực.")]
        public required string Code { get; set; }
    }

    /// <summary>
    /// Body request bật xác thực 2 yếu tố (admin).
    /// </summary>
    public sealed class EnableTwoFactorRequestModel
    {
        // Bug-hunt 2026-10-19: bỏ trường Secret — secret cài đặt do server sinh
        // và giữ trong cache chờ xác thực, client chỉ gửi mã 6 số.
        [Required(ErrorMessage = "Vui lòng nhập mã xác thực.")]
        public required string Code { get; set; }
    }

    /// <summary>
    /// Body request tắt xác thực 2 yếu tố (admin, cần mã xác nhận).
    /// </summary>
    public sealed class DisableTwoFactorRequestModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã xác thực.")]
        public required string Code { get; set; }
    }

    public sealed class LoginResponseModel
    {
        public string? AccessToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        // Nullable: UserName/Email trên AppUser có thể null (Identity cho phép).
        public string? UserName { get; set; }

        // Nullable: Email có thể null khi tài khoản đăng ký bằng SĐT.
        public string? Email { get; set; }

        public string[]? Roles { get; set; }

        /// <summary>Item 98: true when a TOTP code is still required.</summary>
        public bool RequiresTwoFactor { get; set; }

        /// <summary>Item 98: short-lived token for the 2FA verification step.</summary>
        public string? TwoFactorToken { get; set; }

        /// <summary>Item 99: true when this login came from a never-seen IP.</summary>
        public bool NewIpWarning { get; set; }

        /// <summary>Item 99: địa chỉ IP lạ — null khi không có cảnh báo.</summary>
        public string? NewIpAddress { get; set; }
    }

    /// <summary>
    /// Dữ liệu cập nhật hồ sơ tài khoản (tên đăng nhập, email, SĐT).
    /// </summary>
    public class UpdateProfileModel
    {
        /// <summary>Tên đăng nhập mới.</summary>
        public string? UserName { get; set; }

        /// <summary>Email mới.</summary>
        public string? Email { get; set; }

        /// <summary>Số điện thoại mới.</summary>
        public string? PhoneNumber { get; set; }
    }
}
