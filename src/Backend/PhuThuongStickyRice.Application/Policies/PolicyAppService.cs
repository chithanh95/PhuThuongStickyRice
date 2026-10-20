// CS8632: file dùng annotation nullable (?); bật ngữ cảnh annotations để
// compiler hiểu đúng ý nghĩa, không bật phân tích luồng (dự án chưa bật nullable toàn cục).
#nullable enable annotations

using PhuThuongStickyRice.Application.Policies.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Policies
{
    /// <summary>
    /// Wave 6b (item 28): static policy pages at /chinh-sach/{slug},
    /// admin-editable at /admin/chinh-sach.
    /// </summary>
    public interface IPolicyAppService : IApplicationService
    {
        /// <summary>Public: slug + title of every page, for footer links.</summary>
        Task<List<PolicyPageSummaryDto>> GetPublicListAsync();

        /// <summary>Public: one page by slug. Null when the slug is unknown.</summary>
        // [codequality-practices 2026-10-20] Task<PolicyPageDto?>: doc ghi rõ
        // "Null when the slug is unknown" — caller (PoliciesController) đã null-check.
        Task<PolicyPageDto?> GetPublicAsync(string? slug);

        /// <summary>Admin: all pages with bodies.</summary>
        Task<List<PolicyPageDto>> GetListAsync();

        /// <summary>Admin: edit title/body/order (slug is immutable).</summary>
        Task<PolicyPageDto> UpdateAsync(Guid id, UpdatePolicyPageDto dto);
    }

    /// <summary>
    /// Dịch vụ quản lý trang chính sách tĩnh (giao hàng, đổi trả, bảo mật...):
    /// public đọc theo slug cho các link footer tại /chinh-sach/{slug},
    /// admin sửa tiêu đề/nội dung/thứ tự tại /admin/chinh-sach. Slug là bất
    /// biến vì link đã được rải ở footer khắp các trang public. Danh sách
    /// public được cache, admin sửa xong thì xóa cache.
    /// </summary>
    public class PolicyAppService : ApplicationService, IPolicyAppService
    {
        private readonly IRepository<PolicyPage, Guid> _pages;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        /// <summary>Khởi tạo dịch vụ trang chính sách.</summary>
        /// <param name="pages">Repository trang chính sách.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn async.</param>
        /// <param name="catalogCache">Cache dùng chung; invalidate khi trang chính sách thay đổi.</param>
        public PolicyAppService(
            IRepository<PolicyPage, Guid> pages,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _pages = pages;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Public: slug + tiêu đề mọi trang chính sách, để footer các trang
        /// public dựng link /chinh-sach/{slug}. Cache theo danh sách.
        /// </summary>
        /// <returns>Danh sách slug + tiêu đề, sắp xếp theo thứ tự hiển thị.</returns>
        [AllowAnonymous]
        public virtual async Task<List<PolicyPageSummaryDto>> GetPublicListAsync()
        {
            var cached = await _catalogCache.GetOrAddAsync("policies:public-list", async () =>
            {
                var queryable = await _pages.GetQueryableAsync();
                var items = await _asyncExecuter.ToListAsync(
                    queryable.OrderBy(x => x.SortOrder));
                return items.Select(x => new PolicyPageSummaryDto
                {
                    Slug = x.Slug,
                    Title = x.Title
                }).ToList();
            });
            // perf-hunt 2026-10-19: trả bản sao của list trong cache
            // (policies:public-list) — không để caller mutate chung instance
            // cache (pattern AddressBookAppService).
            return new List<PolicyPageSummaryDto>(cached);
        }

        /// <summary>
        /// Public: đọc nội dung một trang chính sách theo slug. Lấy toàn bộ
        /// danh sách 1 lần (cache) rồi lọc trong memory — tránh tạo cache key
        /// cho mỗi slug lạ (bot có thể quét slug không tồn tại).
        /// </summary>
        /// <param name="slug">Slug trang (không phân biệt hoa/thường).</param>
        /// <returns>Trang chính sách; null nếu slug không tồn tại.</returns>
        [AllowAnonymous]
        public virtual async Task<PolicyPageDto?> GetPublicAsync(string? slug)
        {
            var normalized = slug?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            // Perf: cache 1 list toàn bộ trang (5 phút) rồi lọc theo slug trong
            // memory — tránh cache-key explosion khi bot quét slug lạ.
            // So sánh OrdinalIgnoreCase để giữ ngữ nghĩa collation CI của DB.
            var pages = await _catalogCache.GetOrAddAsync("policies:public-pages", async () =>
            {
                var queryable = await _pages.GetQueryableAsync();
                var items = await _asyncExecuter.ToListAsync(
                    queryable.OrderBy(x => x.SortOrder));
                return items.Select(MapToDto).ToList();
            });

            return pages.FirstOrDefault(x =>
                string.Equals(x.Slug, normalized, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Admin: toàn bộ trang chính sách kèm nội dung, để sửa trong màn hình quản trị.</summary>
        /// <returns>Danh sách trang, sắp xếp theo thứ tự hiển thị.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<PolicyPageDto>> GetListAsync()
        {
            var queryable = await _pages.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.SortOrder));
            return items.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: sửa tiêu đề/nội dung/thứ tự của một trang. Slug không đổi
        /// (link footer public đã trỏ theo slug nên đổi slug sẽ gãy link).
        /// Xóa cache public sau khi lưu để trang web thấy ngay.
        /// </summary>
        /// <param name="id">Id trang chính sách.</param>
        /// <param name="dto">Tiêu đề, nội dung, thứ tự hiển thị mới.</param>
        /// <returns>Trang sau khi cập nhật.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<PolicyPageDto> UpdateAsync(Guid id, UpdatePolicyPageDto dto)
        {
            var page = await _pages.GetAsync(id);
            page.Title = dto.Title.Trim();
            page.Body = dto.Body.Trim();
            page.SortOrder = dto.SortOrder;
            await _pages.UpdateAsync(page, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(page);
        }

        private static PolicyPageDto MapToDto(PolicyPage page)
        {
            return new PolicyPageDto
            {
                Id = page.Id,
                Slug = page.Slug,
                Title = page.Title,
                Body = page.Body,
                SortOrder = page.SortOrder
            };
        }
    }
}
