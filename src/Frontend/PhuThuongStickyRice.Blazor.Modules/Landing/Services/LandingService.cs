using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using PhuThuongStickyRice.Blazor.Modules.Landing.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Landing.Services
{
    public class LandingService : HttpService
    {
        public LandingService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<LandingContentModel> GetContentAsync()
        {
            return await GetAsync<LandingContentModel>("api/landing/content");
        }

        public async Task<LandingContentModel> UpdateContentAsync(LandingContentModel model)
        {
            return await PutAsync<LandingContentModel>("api/landing/content", model);
        }
    }
}
