using PhuThuongStickyRice.Domain.Infrastructure.Networking;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Networking
{
    public class FileDownloader : IFileDownloader
    {
        public void DownloadFile(string url, string path)
        {
            var client = new WebClient();
            client.DownloadFile(url, path);
        }

        public async Task DownloadFileAsync(string url, string path, CancellationToken cancellationToken = default)
        {
            var client = new WebClient();
            await client.DownloadFileTaskAsync(url, path);
        }
    }
}
