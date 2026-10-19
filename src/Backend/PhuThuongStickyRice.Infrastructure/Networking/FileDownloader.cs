using PhuThuongStickyRice.Domain.Infrastructure.Networking;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// codequality-team 2026-10-20 run-0245 Agent1-Naming

namespace PhuThuongStickyRice.Infrastructure.Networking
{
    /// <summary>
    /// Tải file từ URL về đĩa. Bản async (<see cref="DownloadFileAsync"/>)
    /// tôn trọng <see cref="CancellationToken"/> của caller; bản sync dùng
    /// cho code legacy không async được.
    /// </summary>
    public class FileDownloader : IFileDownloader
    {
        // Best practice: HttpClient là thread-safe và được thiết kế để tái sử
        // dụng lâu dài — tạo mới mỗi lần gọi gây socket exhaustion (socket ở
        // trạng thái TIME_WAIT). Không có header/timeout riêng cho từng lần
        // gọi nên dùng 1 instance static là tương đương hành vi bản cũ.
        private static readonly HttpClient SharedHttpClient = new HttpClient();

        /// <summary>
        /// Tải file từ <paramref name="url"/> về <paramref name="destinationPath"/>
        /// (ghi đè nếu đã tồn tại). Lỗi HTTP hoặc lỗi mạng ném exception cho
        /// caller xử lý — tương đương <c>WebClient.DownloadFile</c> trước đây.
        /// </summary>
        /// <param name="url">URL của file cần tải.</param>
        /// <param name="destinationPath">Đường dẫn file đích trên đĩa.</param>
        public void DownloadFile(string url, string destinationPath)
        {
            // WebClient đã obsolete từ .NET 6 (SYSLIB0014) và không còn được
            // duy trì — dùng SharedHttpClient dùng chung như bản async.
            // HttpClient không có API đồng bộ thật nên block có kiểm soát trên
            // task async: ASP.NET Core/BackgroundService không có
            // SynchronizationContext nên không deadlock như WinForms/WPF cũ.
            // EnsureSuccessStatusCode giữ nguyên ngữ nghĩa "lỗi HTTP -> ném
            // exception" của WebClient.DownloadFile; stream trực tiếp ra đĩa
            // thay vì nạp cả file vào RAM.
            using var response = SharedHttpClient
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            using var contentStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            contentStream.CopyTo(fileStream);
        }

        /// <summary>
        /// Tải file từ <paramref name="url"/> về <paramref name="destinationPath"/>
        /// (ghi đè nếu đã tồn tại), stream trực tiếp ra đĩa để không tốn RAM.
        /// Hủy giữa chừng qua <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="url">URL của file cần tải.</param>
        /// <param name="destinationPath">Đường dẫn file đích trên đĩa.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            // WebClient.DownloadFileTaskAsync không có overload nhận
            // CancellationToken nên token của caller bị bỏ qua trong im lặng.
            // Dùng HttpClient stream + CopyToAsync để cancellation được tôn trọng.
            await using var responseStream = await SharedHttpClient.GetStreamAsync(url, cancellationToken);
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await responseStream.CopyToAsync(fileStream, cancellationToken);
        }
    }
}
