using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Wave 6d (item 37): in-repo auth actions (change password). The JWT is
    /// attached automatically by HttpService (ITokenManager).
    /// </summary>
    public class AuthService : HttpService
    {
        public AuthService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        /// <summary>
        /// POST api/auth/login. Returns the token payload on success; throws
        /// <see cref="InvalidOperationException"/> with the server's message otherwise.
        /// </summary>
        public async Task<CustomerAuthResult> LoginAsync(string usernameOrEmail, string password)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                new { UsernameOrEmail = usernameOrEmail?.Trim(), Password = password });

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ExtractMessage(body));
            }

            return System.Text.Json.JsonSerializer.Deserialize<CustomerAuthResult>(
                body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>
        /// POST api/auth/register (customer self-registration by phone).
        /// Returns the token payload on success; throws
        /// <see cref="InvalidOperationException"/> with the server's message otherwise.
        /// </summary>
        public async Task<CustomerAuthResult> RegisterAsync(string phone, string password, string fullName)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                new { Phone = phone?.Trim(), Password = password, FullName = fullName?.Trim() });

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ExtractMessage(body));
            }

            return System.Text.Json.JsonSerializer.Deserialize<CustomerAuthResult>(
                body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>
        /// POST api/auth/change-password. Throws <see cref="InvalidOperationException"/>
        /// with the server's message when the change is rejected.
        /// </summary>
        public async Task ChangePasswordAsync(string currentPassword, string newPassword)
        {
            await SetBearerToken();

            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/change-password",
                new { CurrentPassword = currentPassword, NewPassword = newPassword });

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(ExtractMessage(body));
        }

        private static string ExtractMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return "Đổi mật khẩu thất bại. Vui lòng thử lại.";
            }

            var trimmed = body.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    if (doc.RootElement.TryGetProperty("message", out var message)
                        && !string.IsNullOrWhiteSpace(message.GetString()))
                    {
                        return message.GetString();
                    }
                }
                catch (JsonException)
                {
                    // Fall through to raw body.
                }
            }

            return trimmed;
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
}
