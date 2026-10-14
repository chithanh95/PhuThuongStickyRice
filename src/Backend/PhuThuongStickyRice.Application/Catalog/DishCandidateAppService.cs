using PhuThuongStickyRice.Application.Catalog.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Catalog
{
    public interface IDishCandidateAppService : IApplicationService
    {
        /// <summary>Public: active candidates ordered by votes.</summary>
        Task<List<DishCandidateDto>> GetActiveAsync();

        /// <summary>Public: vote for a candidate (one vote per browser is enforced client-side).</summary>
        Task<DishCandidateDto> VoteAsync(Guid id);

        Task<List<DishCandidateDto>> GetListAsync();

        Task<DishCandidateDto> CreateAsync(CreateUpdateDishCandidateDto dto);

        Task<DishCandidateDto> UpdateAsync(Guid id, CreateUpdateDishCandidateDto dto);

        Task DeleteAsync(Guid id);
    }

    public class DishCandidateAppService : ApplicationService, IDishCandidateAppService
    {
        private readonly IRepository<DishCandidate, Guid> _candidates;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public DishCandidateAppService(
            IRepository<DishCandidate, Guid> candidates,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _candidates = candidates;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public async Task<List<DishCandidateDto>> GetActiveAsync()
        {
            var queryable = await _candidates.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive)
                    .OrderByDescending(x => x.Votes)
                    .ThenBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        [AllowAnonymous]
        public async Task<DishCandidateDto> VoteAsync(Guid id)
        {
            var entity = await _candidates.FindAsync(id);
            if (entity == null || !entity.IsActive)
            {
                throw new Domain.Exceptions.DomainException("Món bình chọn không tồn tại hoặc đã đóng.");
            }

            entity.Votes++;
            await _candidates.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<DishCandidateDto>> GetListAsync()
        {
            var queryable = await _candidates.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.Votes).ThenBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<DishCandidateDto> CreateAsync(CreateUpdateDishCandidateDto dto)
        {
            var entity = new DishCandidate
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _candidates.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<DishCandidateDto> UpdateAsync(Guid id, CreateUpdateDishCandidateDto dto)
        {
            var entity = await _candidates.GetAsync(id);

            entity.Name = dto.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            entity.IsActive = dto.IsActive;

            await _candidates.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task DeleteAsync(Guid id)
        {
            await _candidates.DeleteAsync(id);
        }

        private static DishCandidateDto MapToDto(DishCandidate entity)
        {
            return new DishCandidateDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Votes = entity.Votes,
                IsActive = entity.IsActive
            };
        }
    }
}
