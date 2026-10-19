using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>Item 178: public queue-number display (/cho).</summary>
    public class QueueTicketService : ApiClientBase
    {
        public QueueTicketService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Public: today's tickets, no login needed.</summary>
        public async Task<List<QueueTicketModel>> GetNowServingAsync()
        {
            return await GetAsync<List<QueueTicketModel>>("api/queue/now-serving")
                ?? new List<QueueTicketModel>();
        }
    }
}
