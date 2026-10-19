
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Common
{
    public interface IShortLinkAppService : IApplicationService
    {
        /// <summary>
        /// Admin: danh sách link ngắn, mới nhất trước, tối đa 500 dòng.
        /// </summary>
        /// <returns>Danh sách link ngắn.</returns>
        Task<List<ShortLinkDto>> GetListAsync();

        /// <summary>
        /// Tạo (hoặc tái sử dụng) link ngắn cho một URL. Public vì nút "Link
        /// ngắn" hiển thị cho mọi khách; mỗi URL chỉ có một mã duy nhất —
        /// tạo lại URL đã có thì trả về mã cũ thay vì sinh mã mới.
        /// </summary>
        /// <param name="dto">URL đích cần rút gọn.</param>
        /// <returns>Link ngắn (mã + URL đích).</returns>
        Task<ShortLinkDto> CreateAsync(CreateShortLinkDto dto);

        /// <summary>
        /// Admin: xóa một link ngắn.
        /// </summary>
        /// <param name="id">Id link cần xóa.</param>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Public: giải mã thành URL đích để redirect (null khi mã không tồn tại).
        /// </summary>
        /// <param name="code">Mã rút gọn (không phân biệt hoa thường).</param>
        /// <returns>URL đích hoặc null.</returns>
        Task<string> ResolveAsync(string code);
    }

    /// <summary>
    /// Quản lý link rút gọn dạng <c>/s/abc123</c> cho shop: nút "Link ngắn" ở
    /// màn hình xem nhanh sản phẩm tạo link ngắn cho URL sản phẩm để chia sẻ
    /// Zalo/Facebook; truy cập link sẽ chuyển hướng (redirect) về URL gốc.
    /// Mã được sinh ngẫu nhiên để không đoán được, chữ dễ nhầm bị loại bỏ.
    /// </summary>
    public class ShortLinkAppService : ApplicationService, IShortLinkAppService
    {
        /// <summary>
        /// Ký tự dùng sinh mã: loại bỏ l/1/I/o/0 để khách đọc/đánh lại mã
        /// không bị nhầm lẫn ký tự giống nhau.
        /// </summary>
        private const string CodeChars = "abcdefghjkmnpqrstuvwxyz23456789";

        private readonly IRepository<ShortLink, Guid> _links;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IMemoryCache _memoryCache;

        /// <summary>Giới hạn server-side cho màn hình admin (bảng tăng dần).</summary>
        private const int MaxListRows = 500;

        /// <summary>
        /// TTL cache cho redirect public (giống các public path khác: 5 phút).
        /// Chỉ cache HIT (mã tồn tại); MISS không cache để link mới tạo xong
        /// resolve được ngay mà không cần invalidate toàn cục.
        /// </summary>
        private static readonly TimeSpan ResolveCacheTtl = TimeSpan.FromMinutes(5);

        /// <summary>Tiền tố key cache cho <see cref="ResolveAsync"/>.</summary>
        private const string ResolveCacheKeyPrefix = "shortlink:resolve:";

        public ShortLinkAppService(
            IRepository<ShortLink, Guid> links,
            IAsyncQueryableExecuter asyncExecuter,
            IMemoryCache memoryCache)
        {
            _links = links;
            _asyncExecuter = asyncExecuter;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Admin: danh sách link ngắn, mới nhất trước, tối đa 500 dòng
        /// (server-side Take, bảng tăng dần theo thời gian).
        /// </summary>
        /// <returns>Danh sách link ngắn.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<ShortLinkDto>> GetListAsync()
        {
            var queryable = await _links.GetQueryableAsync();
            // perf-hunt 2026-10-19: Take(500) server-side — bảng link ngắn
            // tăng theo thời gian, không bao giờ materialize toàn bộ cho
            // màn hình admin (pattern đã dùng ở RefundAppService.GetReportAsync).
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedDateTime).Take(MaxListRows));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Public (called from the product quick-view "🔗 Link ngắn" button):
        /// returns an existing short link for the URL when one exists, else
        /// creates a fresh 6-char code. Rate-limited at the controller.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<ShortLinkDto> CreateAsync(CreateShortLinkDto dto)
        {
            // Best practice .NET 6+: guard clause chuẩn thay cho if-null-throw thủ công.
            ArgumentNullException.ThrowIfNull(dto);

            var target = dto.TargetUrl.Trim();
            var queryable = await _links.GetQueryableAsync();
            var existing = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.TargetUrl == target));
            if (existing != null)
            {
                return MapToDto(existing);
            }

            var code = await GenerateUniqueCodeAsync();
            var entity = new ShortLink { Code = code, TargetUrl = target };
            await _links.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: xóa một link ngắn. Link đã xóa không redirect được nữa —
        /// xóa luôn key cache của mã này để redirect dừng ngay lập tức
        /// (không chờ hết TTL 5 phút).
        /// </summary>
        /// <param name="id">Id link cần xóa.</param>
        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await _links.GetAsync(id);
            var cacheKey = ResolveCacheKeyPrefix + entity.Code.ToLowerInvariant();
            await _links.DeleteAsync(id, autoSave: true);
            _memoryCache.Remove(cacheKey);
        }

        /// <summary>
        /// Public: giải mã ngắn thành URL đích để redirect. Mã được chuẩn hóa
        /// về chữ thường khi tra cứu nên người dùng gõ hoa/thường đều được.
        /// Kết quả HIT được cache 5 phút (giống các public path khác) vì mỗi
        /// click redirect trước đây đều query DB; MISS không cache.
        /// </summary>
        /// <param name="code">Mã rút gọn (không phân biệt hoa thường).</param>
        /// <returns>URL đích, hoặc null khi mã không tồn tại.</returns>
        [AllowAnonymous]
        public virtual async Task<string> ResolveAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalized = code.Trim().ToLowerInvariant();
            var cacheKey = ResolveCacheKeyPrefix + normalized;
            if (_memoryCache.TryGetValue(cacheKey, out string cachedTarget))
            {
                return cachedTarget;
            }

            var queryable = await _links.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));
            var target = entity?.TargetUrl;
            // Chỉ cache khi tìm thấy: mã mới tạo (CreateAsync sinh mã mới
            // chưa từng cache) resolve được ngay; link bị xóa thì DeleteAsync
            // đã gỡ key nên không bao giờ trả URL cũ quá TTL.
            if (!string.IsNullOrWhiteSpace(target))
            {
                _memoryCache.Set(cacheKey, target, ResolveCacheTtl);
            }

            return target;
        }

        /// <summary>
        /// Sinh mã rút gọn 6 ký tự duy nhất. Thử tối đa 20 lần để tránh trùng;
        /// nếu vẫn trùng (gần như không thể) thì lấy 6 ký tự đầu của GUID để
        /// đảm bảo không bao giờ lỗi khi tạo link.
        /// </summary>
        private async Task<string> GenerateUniqueCodeAsync()
        {
            // Dùng RNG an toàn mật mã thay vì Random.Shared để mã short-link
            // không đoán được từ các mã đã lộ (RandomNumberGenerator.GetInt32
            // là thread-safe nên vẫn an toàn khi nhiều request đồng thời).
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = new string(
                    Enumerable.Range(0, 6).Select(_ => CodeChars[RandomNumberGenerator.GetInt32(CodeChars.Length)]).ToArray());

                var queryable = await _links.GetQueryableAsync();
                var exists = await _asyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.Code == code));
                if (exists == null)
                {
                    return code;
                }
            }

            // Range operator [..6] thay Substring(0, 6): ngắn gọn, đúng idiom C# 8+
            // (cùng cách ProductAppService.Truncate đã dùng), không sợ nhầm
            // startIndex/length.
            return Guid.NewGuid().ToString("N")[..6].ToLowerInvariant();
        }

        private static ShortLinkDto MapToDto(ShortLink entity)
        {
            return new ShortLinkDto
            {
                Id = entity.Id,
                Code = entity.Code,
                TargetUrl = entity.TargetUrl,
                CreatedDateTime = entity.CreatedDateTime
            };
        }
    }
}
