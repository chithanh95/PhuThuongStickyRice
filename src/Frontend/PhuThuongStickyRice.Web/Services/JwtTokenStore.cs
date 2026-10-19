using PhuThuongStickyRice.Web.Components.Core.Services;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Services
{
    /// <summary>
    /// JWT token storage (browser localStorage) for the in-repo admin login.
    /// Tokens are issued by WebAPI itself — no OIDC / external IdentityServer.
    /// </summary>
    public class JwtTokenStore : ITokenStore
    {
        private const string TokenKey = "phuthuong_access_token";
        private const string ExpiresKey = "phuthuong_access_token_expires";

        private readonly IJSRuntime _jsRuntime;

        /// <summary>Khởi tạo kho token với JS runtime để đọc/ghi localStorage trình duyệt.</summary>
        public JwtTokenStore(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>Tokens are attached manually by ApiClientBase, not by a message handler.</summary>
        public bool AttachTokenAutomatically => false;

        /// <summary>
        /// Đọc token đang lưu trong localStorage. Trả null khi chưa đăng nhập
        /// hoặc token đã bị xóa (trang gọi sẽ chuyển về login).
        /// </summary>
        /// <returns>Cặp token + thời điểm hết hạn; null nếu không có token.</returns>
        public async Task<AuthTokenPair> GetToken()
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var expiresText = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ExpiresKey);
            // Kết quả parse bị bỏ qua có chủ ý: token hỏng định dạng thời gian
            // thì ExpiresAt = default (MinValue) và logic gọi phía trên tự coi
            // như hết hạn — giữ nguyên hành vi cũ, chỉ làm rõ ý định (CA1806).
            _ = DateTimeOffset.TryParse(expiresText, out var expiresAt);

            return new AuthTokenPair
            {
                AccessToken = token,
                ExpiresAt = expiresAt
            };
        }

        /// <summary>No refresh tokens — the admin logs in again when the token expires.</summary>
        public Task RefreshToken() => Task.CompletedTask;

        /// <summary>
        /// Lưu token sau khi đăng nhập thành công. Thời điểm hết hạn lưu
        /// định dạng "O" (round-trip) để đọc lại chính xác múi giờ.
        /// </summary>
        /// <param name="token">Chuỗi JWT do WebAPI cấp.</param>
        /// <param name="expiresAt">Thời điểm token hết hạn.</param>
        public async Task StoreTokenAsync(string token, DateTimeOffset expiresAt)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ExpiresKey, expiresAt.ToString("O"));
        }

        /// <summary>
        /// Xóa token khỏi localStorage khi đăng xuất — các request sau đó
        /// không còn gắn token và trang sẽ chuyển về login.
        /// </summary>
        public async Task ClearTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ExpiresKey);
        }
    }
}
