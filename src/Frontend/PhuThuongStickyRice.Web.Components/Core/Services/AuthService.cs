using PhuThuongStickyRice.Web.Components.Shop.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Wave 6d (item 37): in-repo auth actions (change password). The JWT is
    /// attached automatically by ApiClientBase (ITokenStore).
    /// </summary>
    public class AuthService : ApiClientBase
    {
        // CA1869: reuse one options instance instead of allocating per call.
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Khởi tạo service xác thực với HttpClient và nơi lưu token đăng nhập.</summary>
        public AuthService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// POST api/auth/login. Returns the token payload on success; throws
        /// <see cref="InvalidOperationException"/> with the server's message otherwise.
        /// </summary>
        public async Task<CustomerAuthResult> LoginAsync(string usernameOrEmail, string password)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                new { UsernameOrEmail = usernameOrEmail?.Trim(), Password = password });

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ExtractMessage(body));
            }

            return System.Text.Json.JsonSerializer.Deserialize<CustomerAuthResult>(
                body,
                CaseInsensitiveOptions);
        }

        /// <summary>
        /// POST api/auth/register (customer self-registration by phone).
        /// Returns the token payload on success; throws
        /// <see cref="InvalidOperationException"/> with the server's message otherwise.
        /// </summary>
        public async Task<CustomerAuthResult> RegisterAsync(string phone, string password, string fullName)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                new { Phone = phone?.Trim(), Password = password, FullName = fullName?.Trim() });

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ExtractMessage(body));
            }

            return System.Text.Json.JsonSerializer.Deserialize<CustomerAuthResult>(
                body,
                CaseInsensitiveOptions);
        }

        /// <summary>
        /// POST api/auth/change-password. Throws <see cref="InvalidOperationException"/>
        /// with the server's message when the change is rejected.
        /// </summary>
        public async Task ChangePasswordAsync(string currentPassword, string newPassword)
        {
            await SetBearerToken();

            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/change-password",
                new { CurrentPassword = currentPassword, NewPassword = newPassword });

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(ExtractMessage(body));
        }
        /// <summary>
        /// GET api/auth/profile: hồ sơ tài khoản đang đăng nhập (tên, email,
        /// SĐT). Cần token — gắn tự động từ <see cref="ITokenStore"/>.
        /// </summary>
        public async Task<UserProfile> GetProfileAsync()
        {
            await SetBearerToken();
            return await _httpClient.GetFromJsonAsync<UserProfile>("api/auth/profile");
        }

        /// <summary>
        /// PUT api/auth/profile: cập nhật tên hiển thị, email, SĐT của tài
        /// khoản đang đăng nhập. Ném <see cref="InvalidOperationException"/>
        /// kèm message server khi bị từ chối (vd SĐT đã dùng).
        /// </summary>
        public async Task UpdateProfileAsync(string userName, string email, string phoneNumber)
        {
            await SetBearerToken();
            using var response = await _httpClient.PutAsJsonAsync(
                "api/auth/profile",
                new { UserName = userName, Email = email, PhoneNumber = phoneNumber });
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(ExtractMessage(body));
        }

        /// <summary>Staff team chat: recent messages (oldest first).</summary>
        public async Task<List<StaffChatMessage>> GetStaffChatAsync()
        {
            await SetBearerToken();
            return await _httpClient.GetFromJsonAsync<List<StaffChatMessage>>("api/staff-chat")
                ?? new List<StaffChatMessage>();
        }

        /// <summary>Staff team chat: send a message.</summary>
        public async Task SendStaffChatAsync(string message)
        {
            await SetBearerToken();
            using var response = await _httpClient.PostAsJsonAsync("api/staff-chat", new { Message = message });
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(ExtractMessage(body));
        }


        /// <summary>
        /// POST api/auth/forgot-password/request: yêu cầu gửi OTP 6 số qua SMS
        /// về SĐT đăng ký (OTP hiệu lực 5 phút, giới hạn 3 req/giờ). Ném
        /// <see cref="InvalidOperationException"/> kèm message server khi bị
        /// từ chối (vd quá giới hạn yêu cầu).
        /// </summary>
        public async Task RequestPasswordResetOtpAsync(string phone)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/forgot-password/request",
                new { Phone = phone });

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(ExtractMessage(body));
        }

        /// <summary>
        /// POST api/auth/forgot-password/reset: đặt lại mật khẩu mới bằng OTP
        /// đã gửi. OTP sai quá 3 lần thì khóa — ném
        /// <see cref="InvalidOperationException"/> kèm message server.
        /// </summary>
        public async Task ResetPasswordWithOtpAsync(string phone, string otp, string newPassword)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/forgot-password/reset",
                new { Phone = phone, Otp = otp, NewPassword = newPassword });

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(ExtractMessage(body));
        }

        private static string ExtractMessage(string body)
        {
            // codequality-team 2026-10-20: dùng chung JsonDocument parser ở
            // HttpErrorMessageReader thay vì copy logic trích "message" từ JSON.
            var message = HttpErrorMessageReader.ExtractJsonMessage(body);
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            var trimmed = body?.Trim();
            return string.IsNullOrEmpty(trimmed)
                ? "Đổi mật khẩu thất bại. Vui lòng thử lại."
                : trimmed;
        }
    }

    /// <summary>Token payload returned by api/auth/login and api/auth/register.</summary>
    public sealed class CustomerAuthResult
    {
        public string AccessToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string[] Roles { get; set; }
    }

    public class UserProfile
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class StaffChatMessage
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
    }
}
