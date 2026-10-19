using PhuThuongStickyRice.Application.Faq.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Faq
{
    // [codequality-docs 2026-10-19]

    /// <summary>Hợp đồng dịch vụ quản lý câu hỏi thường gặp (FAQ) của quán.</summary>
    public interface IFaqAppService : IApplicationService
    {
        /// <summary>Public: active FAQs, optionally filtered by a search term.</summary>
        Task<List<FaqItemDto>> GetPublicAsync(string searchTerm);

        /// <summary>Admin: all FAQs.</summary>
        Task<List<FaqItemDto>> GetListAsync();

        /// <summary>Admin: create.</summary>
        Task<FaqItemDto> CreateAsync(CreateFaqItemDto dto);

        /// <summary>Admin: update.</summary>
        Task<FaqItemDto> UpdateAsync(Guid id, CreateFaqItemDto dto);

        /// <summary>Admin: delete.</summary>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Dịch vụ quản lý câu hỏi thường gặp (FAQ) của quán: phục vụ danh sách FAQ
    /// công khai cho trang FAQ và cung cấp bộ CRUD cho admin. Danh sách công khai
    /// được cache vì trang FAQ được truy cập trên mọi lượt xem.
    /// </summary>
    public class FaqAppService : ApplicationService, IFaqAppService
    {
        private readonly IRepository<FaqItem, Guid> _faqs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        /// <summary>Khởi tạo dịch vụ FAQ.</summary>
        /// <param name="faqs">Repository entity FAQ.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn async.</param>
        /// <param name="catalogCache">Cache dùng chung; invalidate khi FAQ thay đổi.</param>
        public FaqAppService(
            IRepository<FaqItem, Guid> faqs,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _faqs = faqs;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        [AllowAnonymous]
        public virtual async Task<List<FaqItemDto>> GetPublicAsync(string searchTerm)
        {
            // perf-hunt 2026-10-19: GET /api/faqs is public and hit on every
            // FAQ page view. The active list is tiny (admin-managed), so cache
            // it once and apply the search term in memory instead of one
            // cache key per term (avoids filling the cache with junk terms).
            var all = await _catalogCache.GetOrAddAsync("faqs:public-list", async () =>
            {
                var queryable = await _faqs.GetQueryableAsync();
                var items = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.IsActive).OrderBy(x => x.SortOrder));
                return items.Select(MapToDto).ToList();
            });

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return all;
            }

            var term = searchTerm.Trim();
            return all.Where(x =>
                    x.Question.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Answer.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<FaqItemDto>> GetListAsync()
        {
            var queryable = await _faqs.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.SortOrder));
            return items.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<FaqItemDto> CreateAsync(CreateFaqItemDto dto)
        {
            var entity = new FaqItem
            {
                Question = dto.Question.Trim(),
                Answer = dto.Answer.Trim(),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };
            await _faqs.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<FaqItemDto> UpdateAsync(Guid id, CreateFaqItemDto dto)
        {
            var entity = await _faqs.GetAsync(id);
            entity.Question = dto.Question.Trim();
            entity.Answer = dto.Answer.Trim();
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;
            await _faqs.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            await _faqs.DeleteAsync(id, autoSave: true);
            _catalogCache.Invalidate();
        }

        private static FaqItemDto MapToDto(FaqItem entity)
        {
            return new FaqItemDto
            {
                Id = entity.Id,
                Question = entity.Question,
                Answer = entity.Answer,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive
            };
        }
    }
}
