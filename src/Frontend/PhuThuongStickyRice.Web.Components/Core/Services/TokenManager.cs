using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    public interface ITokenManager
    {
        bool AttachTokenAutomatically { get; }

        Task<TokenModel> GetToken();

        Task RefreshToken();

        /// <summary>Stores a newly issued access token (e.g. after login/register).</summary>
        Task StoreTokenAsync(string accessToken, DateTimeOffset expiresAt);

        /// <summary>Clears the stored token (logout).</summary>
        Task ClearTokenAsync();
    }

    public class TokenModel
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public bool TokenExpired
        {
            get
            {
                return ExpiresAt.ToUniversalTime() <= DateTime.UtcNow.AddSeconds(60);
            }
        }
    }
}
