using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Kiểm tra chữ ký file (magic bytes) của ảnh upload — chống giả mạo
    /// định dạng: kẻ tấn công đổi tên file thực thi/script thành ".jpg" vẫn
    /// qua được bước kiểm tra đuôi file. Chỉ các định dạng ảnh thật mới có
    /// magic bytes đúng:
    /// <list type="bullet">
    /// <item>JPEG: FF D8 FF</item>
    /// <item>PNG: 89 50 4E 47 0D 0A 1A 0A</item>
    /// <item>GIF: "GIF87a" hoặc "GIF89a"</item>
    /// <item>WebP: "RIFF"...."WEBP" (RIFF container)</item>
    /// </list>
    /// Đọc tối đa 12 byte đầu qua <see cref="IFormFile.OpenReadStream"/> —
    /// mỗi lần gọi trả về stream mới ở vị trí 0 nên không ảnh hưởng lần
    /// <c>CopyToAsync</c> lưu file sau đó.
    /// </summary>
    public static class ImageUploadValidator
    {
        /// <summary>
        /// Trả về true khi 12 byte đầu của file khớp magic bytes của
        /// <paramref name="extension"/> (so khớp đuôi file với nội dung thật).
        /// </summary>
        /// <param name="file">File ảnh vừa upload.</param>
        /// <param name="extension">Đuôi file đã chuẩn hóa (vd ".jpg").</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public static async Task<bool> HasValidImageSignatureAsync(
            IFormFile file, string extension, CancellationToken cancellationToken = default)
        {
            if (file == null || string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            var header = new byte[12];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
                if (read < 4)
                {
                    return false;
                }
            }

            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    // JPEG: FF D8 FF (byte thứ 4 là marker tiếp theo).
                    return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

                case ".png":
                    // PNG: 89 50 4E 47 0D 0A 1A 0A.
                    return header[0] == 0x89 && header[1] == 0x50
                        && header[2] == 0x4E && header[3] == 0x47;

                case ".gif":
                    // GIF: "GIF87a" / "GIF89a" — chỉ cần 6 byte đầu.
                    return header[0] == (byte)'G' && header[1] == (byte)'I'
                        && header[2] == (byte)'F' && header[3] == (byte)'8'
                        && (header[4] == (byte)'7' || header[4] == (byte)'9')
                        && header[5] == (byte)'a';

                case ".webp":
                    // WebP: "RIFF" + 4 byte size + "WEBP" — cần đủ 12 byte.
                    return header[0] == (byte)'R' && header[1] == (byte)'I'
                        && header[2] == (byte)'F' && header[3] == (byte)'F'
                        && header[8] == (byte)'W' && header[9] == (byte)'E'
                        && header[10] == (byte)'B' && header[11] == (byte)'P';

                default:
                    return false;
            }
        }
    }
}
