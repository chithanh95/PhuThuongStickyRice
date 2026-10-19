using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// GL1: đo phễu chuyển đổi ẩn danh. Gửi sự kiện page-view tới
    /// <c>api/funnel-events</c> theo kiểu fire-and-forget — lỗi tracking không
    /// bao giờ làm vỡ trang. Không lưu PII: chỉ gửi tên sự kiện trong danh
    /// sách cho phép + đường dẫn URL (đã cắt query/fragment).
    /// </summary>
    public class FunnelService : ApiClientBase
    {
        /// <summary>Khởi tạo service đo phễu với HttpClient và token store.</summary>
        public FunnelService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Tên các sự kiện phễu được phép gửi (không chứa PII).</summary>
        public static class Events
        {
            /// <summary>Khách mở trang landing.</summary>
            public const string LandingView = "landing_view";

            /// <summary>Khách mở trang shop (thực đơn).</summary>
            public const string ShopView = "shop_view";

            /// <summary>Khách mở giỏ hàng.</summary>
            public const string CartView = "cart_view";

            /// <summary>Khách mở trang thanh toán.</summary>
            public const string CheckoutView = "checkout_view";

            /// <summary>Đơn hàng được đặt thành công.</summary>
            public const string OrderPlaced = "order_placed";

            /// <summary>Khách mở trang tra cứu đơn.</summary>
            public const string TrackView = "track_view";
        }

        /// <summary>
        /// Gửi sự kiện phễu theo kiểu fire-and-forget — không bao giờ throw.
        /// Trang gọi <c>Track</c> không cần await nên render không bị chặn
        /// bởi network tracking; lỗi (kể cả API chết) bị nuốt trong TrackAsync.
        /// </summary>
        /// <param name="eventName">Tên sự kiện (dùng <see cref="Events"/>).</param>
        /// <param name="url">URL hiện tại; null thì gửi chuỗi rỗng.</param>
        public void Track(string eventName, string url = null)
        {
            _ = TrackAsync(eventName, url);
        }

        /// <summary>
        /// Gửi 1 sự kiện phễu về API. Query string và fragment bị cắt bỏ
        /// trước khi gửi — URL có thể chứa SĐT/mã đơn trong query, cắt đi để
        /// tracking giữ đúng cam kết "không PII".
        /// </summary>
        private async Task TrackAsync(string eventName, string url)
        {
            try
            {
                var path = url ?? string.Empty;
                // Path only — strip query/fragment (no PII).
                var queryIndex = path.IndexOf('?');
                if (queryIndex >= 0) path = path.Substring(0, queryIndex);
                var hashIndex = path.IndexOf('#');
                if (hashIndex >= 0) path = path.Substring(0, hashIndex);

                await PostAsync<object>("api/funnel-events", new
                {
                    eventName,
                    url = path,
                });
            }
            catch (Exception)
            {
                // Fail-soft: tracking must never break the shop.
            }
        }

        /// <summary>Admin: báo cáo phễu chuyển đổi (lượt xem theo từng bước).</summary>
        /// <returns>Mỗi dòng là 1 sự kiện với số lượt hôm nay và 7 ngày qua.</returns>
        public async Task<System.Collections.Generic.List<FunnelReportRow>> GetReportAsync()
        {
            return await GetAsync<System.Collections.Generic.List<FunnelReportRow>>("api/funnel-events/report");
        }

        /// <summary>Một dòng báo cáo phễu: số lượt của 1 sự kiện.</summary>
        public class FunnelReportRow
        {
            /// <summary>Tên sự kiện (xem <see cref="Events"/>).</summary>
            public string EventName { get; set; }

            /// <summary>Số lượt trong hôm nay.</summary>
            public long Today { get; set; }

            /// <summary>Số lượt trong 7 ngày qua.</summary>
            public long Last7Days { get; set; }
        }
    }
}
