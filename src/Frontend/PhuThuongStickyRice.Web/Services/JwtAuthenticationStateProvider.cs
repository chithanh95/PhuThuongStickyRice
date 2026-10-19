using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Services
{
    /// <summary>
    /// Builds the Blazor auth state from the stored self-issued JWT
    /// (claims are read from the token payload — the API is the authority
    /// that validates the signature on every request).
    /// </summary>
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly JwtTokenStore _tokenStore;

        /// <summary>Khởi tạo provider trạng thái đăng nhập với kho lưu token của app.</summary>
        public JwtAuthenticationStateProvider(JwtTokenStore tokenStore)
        {
            _tokenStore = tokenStore;
        }

        /// <summary>
        /// Dựng trạng thái đăng nhập của Blazor từ JWT đã lưu: không có token
        /// → anonymous; có token → đọc claims từ payload (chỉ để hiển thị,
        /// API vẫn là nơi xác thực chữ ký thật trên mỗi request).
        /// </summary>
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _tokenStore.GetToken();
            if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var identity = new ClaimsIdentity(ParseClaims(token.AccessToken), "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        /// <summary>
        /// Báo cho Blazor biết vừa đăng nhập xong (đọc lại token từ kho) để
        /// toàn bộ trang/menu cập nhật trạng thái đăng nhập ngay lập tức.
        /// </summary>
        public void NotifyUserAuthentication()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        /// <summary>
        /// Báo cho Blazor biết vừa đăng xuất — đẩy trạng thái anonymous để
        /// UI chuyển ngay về dạng chưa đăng nhập (token đã bị xóa ở
        /// <see cref="JwtTokenStore.ClearTokenAsync"/>).
        /// </summary>
        public void NotifyUserLogout()
        {
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        internal static IEnumerable<Claim> ParseClaims(string jwt)
        {
            var claims = new List<Claim>();
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3)
                {
                    return claims;
                }

                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                using var doc = JsonDocument.Parse(payloadJson);
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    var values = new List<string>();
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                values.Add(item.GetString());
                            }
                        }
                    }
                    else if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        values.Add(property.Value.GetString());
                    }
                    else if (property.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                    {
                        values.Add(property.Value.GetRawText());
                    }

                    foreach (var value in values)
                    {
                        claims.Add(new Claim(MapClaimType(property.Name), value));
                    }
                }
            }
            catch (Exception)
            {
                // Malformed token — treat as anonymous; the API still
                // validates the signature on every request.
            }

            return claims;
        }

        private static string MapClaimType(string jwtName) => jwtName switch
        {
            "sub" => ClaimTypes.NameIdentifier,
            "name" => ClaimTypes.Name,
            "email" => ClaimTypes.Email,
            "role" => ClaimTypes.Role,
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" => ClaimTypes.Role,
            _ => jwtName
        };

        private static byte[] Base64UrlDecode(string input)
        {
            var base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
