using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Item 164: gọi API thẻ xôi sáng trả trước từ phía shop (mua thẻ + tra cứu theo SĐT).
    /// Client HTTP mỏng — mọi nghiệp vụ (tính giá gói, sinh mã thẻ, trừ suất) nằm ở backend
    /// <c>PrepaidCardAppService</c>.
    /// </summary>
    public class PrepaidCardService : ApiClientBase
    {
        /// <summary>Khởi tạo service với HttpClient và token manager dùng chung.</summary>
        public PrepaidCardService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Mua một gói thẻ trả trước (20 hoặc 30 suất). Không cần đăng nhập.
        /// </summary>
        /// <param name="request">Tên, SĐT khách và gói suất muốn mua.</param>
        /// <returns>Thông tin thẻ vừa tạo, gồm mã thẻ để khách giữ.</returns>
        public async Task<PrepaidCardModel> BuyAsync(BuyPrepaidCardRequest request)
        {
            return await PostAsync<PrepaidCardModel>("api/prepaid-cards/buy", request);
        }

        /// <summary>
        /// Tra cứu các thẻ của một SĐT (mới nhất trước) để khách xem số suất còn lại.
        /// </summary>
        /// <param name="phone">SĐT đã đăng ký thẻ.</param>
        public async Task<List<PrepaidCardModel>> GetMyCardsAsync(string phone)
        {
            return await GetAsync<List<PrepaidCardModel>>(
                $"api/prepaid-cards/my?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }
    }
}
