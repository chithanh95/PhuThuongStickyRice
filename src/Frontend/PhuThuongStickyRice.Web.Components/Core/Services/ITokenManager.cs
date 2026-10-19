using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Hợp đồng lưu trữ/truy xuất JWT ở phía Blazor WASM.
    /// Cài đặt cụ thể: <c>JwtTokenStore</c> (project PhuThuongStickyRice.Web).
    /// </summary>
    public interface ITokenManager
    {
        bool AttachTokenAutomatically { get; }

        Task<AuthTokenPair> GetToken();

        Task RefreshToken();

        /// <summary>Stores a newly issued access token (e.g. after login/register).</summary>
        Task StoreTokenAsync(string accessToken, DateTimeOffset expiresAt);

        /// <summary>Clears the stored token (logout).</summary>
        Task ClearTokenAsync();
    }
}
