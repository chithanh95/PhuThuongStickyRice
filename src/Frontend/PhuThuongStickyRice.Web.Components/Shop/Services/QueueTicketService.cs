using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Item 178: client gọi API màn hình số thứ tự công khai (/cho).
    /// Trang này mở cho mọi khách tại quán xem số đang được phục vụ mà
    /// không cần đăng nhập — là màn hình chờ chung nên chỉ đọc, không
    /// bao giờ ghi dữ liệu.
    /// </summary>
    public class QueueTicketService : ApiClientBase
    {
        /// <summary>Khởi tạo client gọi API hàng đợi.</summary>
        /// <param name="httpClient">HttpClient đã cấu hình base address API.</param>
        /// <param name="tokenStore">Kho token (trang này không yêu cầu login; token chỉ được gắn nếu có session).</param>
        public QueueTicketService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Lấy danh sách phiếu đang được phục vụ hôm nay (public, không cần login).
        /// </summary>
        /// <returns>
        /// Danh sách phiếu hôm nay; rỗng khi API không trả dữ liệu (null).
        /// </returns>
        public async Task<List<QueueTicketModel>> GetNowServingAsync()
        {
            return await GetAsync<List<QueueTicketModel>>("api/queue/now-serving")
                ?? new List<QueueTicketModel>();
        }
    }
}
