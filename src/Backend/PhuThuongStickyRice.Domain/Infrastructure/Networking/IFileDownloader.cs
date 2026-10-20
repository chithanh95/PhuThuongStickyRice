using System.Threading;
using System.Threading.Tasks;

// codequality-team 2026-10-20 run-0245 Agent1-Naming

namespace PhuThuongStickyRice.Domain.Infrastructure.Networking
{
    /// <summary>
    /// Tải file từ URL về đường dẫn local. Dùng cho các tác vụ nền cần lấy
    /// tài nguyên ngoài (sao lưu ảnh sản phẩm, tải tài liệu) mà không để Domain
    /// phụ thuộc trực tiếp vào HttpClient hay thư viện mạng cụ thể.
    /// </summary>
    public interface IFileDownloader
    {
        /// <summary>Tải file đồng bộ từ <paramref name="url"/> về <paramref name="destinationPath"/>.</summary>
        /// <param name="url">Địa chỉ file nguồn.</param>
        /// <param name="destinationPath">Đường dẫn local lưu file (ghi đè nếu đã tồn tại).</param>
        void DownloadFile(string url, string destinationPath);

        /// <summary>Tải file bất đồng bộ từ <paramref name="url"/> về <paramref name="destinationPath"/>.</summary>
        /// <param name="url">Địa chỉ file nguồn.</param>
        /// <param name="destinationPath">Đường dẫn local lưu file (ghi đè nếu đã tồn tại).</param>
        /// <param name="cancellationToken">Token hủy tác vụ tải.</param>
        Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default);
    }
}