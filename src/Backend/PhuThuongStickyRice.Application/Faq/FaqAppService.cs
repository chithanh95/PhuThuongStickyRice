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

    public class FaqAppService : ApplicationService, IFaqAppService
    {
        private readonly IRepository<FaqItem, Guid> _faqs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public FaqAppService(
            IRepository<FaqItem, Guid> faqs,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _faqs = faqs;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public virtual async Task<List<FaqItemDto>> GetPublicAsync(string searchTerm)
        {
            var queryable = await _faqs.GetQueryableAsync();
            IQueryable<FaqItem> query = queryable
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(x =>
                    x.Question.Contains(term) || x.Answer.Contains(term));
            }

            var items = await _asyncExecuter.ToListAsync(query);
            return items.Select(MapToDto).ToList();
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
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            await _faqs.DeleteAsync(id, autoSave: true);
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
