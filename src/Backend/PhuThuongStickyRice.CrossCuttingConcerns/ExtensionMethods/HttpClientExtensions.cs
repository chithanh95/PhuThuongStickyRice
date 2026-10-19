using System.Net.Http;
using System.Net.Http.Headers;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    public static class HttpClientExtensions
    {
        public static void UseBearerToken(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
