using PhuThuongStickyRice.Domain.Infrastructure.Networking;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Networking
{
    public class FileDownloader : IFileDownloader
    {
        public void DownloadFile(string url, string path)
        {
            // WebClient là IDisposable — using var để đóng handle/socket ngay khi xong.
            using var client = new WebClient();
            client.DownloadFile(url, path);
        }

        public async Task DownloadFileAsync(string url, string path, CancellationToken cancellationToken = default)
        {
            // WebClient.DownloadFileTaskAsync không có overload nhận
            // CancellationToken nên token của caller bị bỏ qua trong im lặng.
            // Dùng HttpClient stream + CopyToAsync để cancellation được tôn trọng.
            using var client = new HttpClient();
            await using var responseStream = await client.GetStreamAsync(url, cancellationToken);
            await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await responseStream.CopyToAsync(fileStream, cancellationToken);
        }
    }
}
