using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// GL1: anonymous funnel measurement. Posts page-view events to
    /// <c>api/funnel-events</c> fire-and-forget — failures never break the page.
    /// No PII: only the allowlisted event name + URL path.
    /// </summary>
    public class FunnelService : ApiClientBase
    {
        public FunnelService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        public static class Events
        {
            public const string LandingView = "landing_view";
            public const string ShopView = "shop_view";
            public const string CartView = "cart_view";
            public const string CheckoutView = "checkout_view";
            public const string OrderPlaced = "order_placed";
            public const string TrackView = "track_view";
        }

        /// <summary>Fire-and-forget: never throws.</summary>
        public void Track(string eventName, string url = null)
        {
            _ = TrackAsync(eventName, url);
        }

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

        public async Task<System.Collections.Generic.List<FunnelReportRow>> GetReportAsync()
        {
            return await GetAsync<System.Collections.Generic.List<FunnelReportRow>>("api/funnel-events/report");
        }

        public class FunnelReportRow
        {
            public string EventName { get; set; }
            public long Today { get; set; }
            public long Last7Days { get; set; }
        }
    }
}
