
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.Application.Shop
{
    public interface IBranchAppService : IApplicationService
    {
        Task<List<BranchDto>> GetListAsync();
        Task<List<BranchDto>> GetActiveAsync();
        Task<BranchDto> GetAsync(Guid id);
        Task<BranchDto> CreateAsync(CreateUpdateBranchDto dto);
        Task<BranchDto> UpdateAsync(Guid id, CreateUpdateBranchDto dto);
        Task DeleteAsync(Guid id);
    }

    public class BranchAppService : ApplicationService, IBranchAppService
    {
        private readonly IRepository<Branch, Guid> _branches;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public BranchAppService(
            IRepository<Branch, Guid> branches,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _branches = branches;
            _asyncExecuter = asyncExecuter;
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<BranchDto>> GetListAsync()
        {
            var queryable = await _branches.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        [AllowAnonymous]
        public virtual async Task<List<BranchDto>> GetActiveAsync()
        {
            var queryable = await _branches.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive).OrderBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<BranchDto> GetAsync(Guid id)
        {
            return MapToDto(await _branches.GetAsync(id));
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<BranchDto> CreateAsync(CreateUpdateBranchDto dto)
        {
            var entity = new Branch
            {
                Name = dto.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                IsActive = dto.IsActive
            };
            await _branches.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<BranchDto> UpdateAsync(Guid id, CreateUpdateBranchDto dto)
        {
            var entity = await _branches.GetAsync(id);
            entity.Name = dto.Name.Trim();
            entity.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            entity.IsActive = dto.IsActive;
            await _branches.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            await _branches.DeleteAsync(id, autoSave: true);
        }

        private static BranchDto MapToDto(Branch entity)
        {
            return new BranchDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Address = entity.Address,
                Phone = entity.Phone,
                IsActive = entity.IsActive
            };
        }
    }
}
