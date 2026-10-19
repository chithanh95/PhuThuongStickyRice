using System.Threading;
using System.Threading.Tasks;

// codequality-team 2026-10-20 run-0245 Agent1-Naming

namespace PhuThuongStickyRice.Domain.Infrastructure.Networking
{
    public interface IFileDownloader
    {
        void DownloadFile(string url, string destinationPath);

        Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default);
    }
}