
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>Giới hạn server-side cho màn hình admin (bảng tăng dần).</summary>
        private const int MaxListRows = 500;

        public ShortLinkAppService(
            IRepository<ShortLink, Guid> links,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _links = links;
            _asyncExecuter = asyncExecuter;
        }

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

        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            await _links.DeleteAsync(id, autoSave: true);
        }

        [AllowAnonymous]
        public virtual async Task<string> ResolveAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalized = code.Trim().ToLowerInvariant();
            var queryable = await _links.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));
            return entity?.TargetUrl;
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

            return Guid.NewGuid().ToString("N").Substring(0, 6).ToLowerInvariant();
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
