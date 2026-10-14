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
        Task<PolicyPageDto> GetPublicAsync(string slug);

        /// <summary>Admin: all pages with bodies.</summary>
        Task<List<PolicyPageDto>> GetListAsync();

        /// <summary>Admin: edit title/body/order (slug is immutable).</summary>
        Task<PolicyPageDto> UpdateAsync(Guid id, UpdatePolicyPageDto dto);
    }

    public class PolicyAppService : ApplicationService, IPolicyAppService
    {
        private readonly IRepository<PolicyPage, Guid> _pages;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public PolicyAppService(
            IRepository<PolicyPage, Guid> pages,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _pages = pages;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public async Task<List<PolicyPageSummaryDto>> GetPublicListAsync()
        {
            var queryable = await _pages.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.SortOrder));
            return items.Select(x => new PolicyPageSummaryDto
            {
                Slug = x.Slug,
                Title = x.Title
            }).ToList();
        }

        [AllowAnonymous]
        public async Task<PolicyPageDto> GetPublicAsync(string slug)
        {
            var normalized = slug?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            var queryable = await _pages.GetQueryableAsync();
            var page = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Slug == normalized));
            return page == null ? null : MapToDto(page);
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<PolicyPageDto>> GetListAsync()
        {
            var queryable = await _pages.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.SortOrder));
            return items.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<PolicyPageDto> UpdateAsync(Guid id, UpdatePolicyPageDto dto)
        {
            var page = await _pages.GetAsync(id);
            page.Title = dto.Title.Trim();
            page.Body = dto.Body.Trim();
            page.SortOrder = dto.SortOrder;
            await _pages.UpdateAsync(page, autoSave: true);
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
