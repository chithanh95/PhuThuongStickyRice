
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Hợp đồng quản lý chi nhánh: admin CRUD đầy đủ; trang shop gọi
    /// <c>GetActiveAsync</c> (không cần login) để khách chọn chi nhánh nhận hàng.
    /// </summary>
    public interface IBranchAppService : IApplicationService
    {
        /// <summary>Admin: liệt kê toàn bộ chi nhánh, sắp xếp theo tên.</summary>
        Task<List<BranchDto>> GetListAsync();

        /// <summary>Public: chi nhánh đang hoạt động (danh sách nhỏ, cache 5 phút).</summary>
        Task<List<BranchDto>> GetActiveAsync();

        /// <summary>Admin: xem chi tiết một chi nhánh.</summary>
        Task<BranchDto> GetAsync(Guid id);

        /// <summary>Admin: tạo chi nhánh mới.</summary>
        Task<BranchDto> CreateAsync(CreateUpdateBranchDto dto);

        /// <summary>Admin: cập nhật thông tin chi nhánh.</summary>
        Task<BranchDto> UpdateAsync(Guid id, CreateUpdateBranchDto dto);

        /// <summary>Admin: xóa chi nhánh.</summary>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Quản lý chi nhánh của quán. Admin CRUD đầy đủ (mỗi thay đổi đều xóa cache
    /// danh sách đang hoạt động để trang shop thấy ngay); khách hàng xem danh sách
    /// chi nhánh đang hoạt động qua <c>GetActiveAsync</c> (public, cache 5 phút).
    /// </summary>
    public class BranchAppService : ApplicationService, IBranchAppService
    {
        private readonly IRepository<Branch, Guid> _branches;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        public BranchAppService(
            IRepository<Branch, Guid> branches,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _branches = branches;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Admin: liệt kê toàn bộ chi nhánh, sắp xếp theo tên (kể cả chi nhánh đã tắt).
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<BranchDto>> GetListAsync()
        {
            var queryable = await _branches.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Public: chi nhánh đang hoạt động cho khách chọn nơi nhận hàng.
        /// Kết quả được cache 5 phút; mọi CRUD của admin đều invalidate cache
        /// để shop thấy thay đổi ngay.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<List<BranchDto>> GetActiveAsync()
        {
            // Danh sách chi nhánh đang hoạt động rất ngắn nhưng được gọi public
            // (khách chọn chi nhánh ở trang shop) nên cache 5 phút để đỡ tải
            // DB; mỗi thao tác tạo/sửa/xóa của admin bên dưới đều invalidate.
            return await _catalogCache.GetOrAddAsync("branches:active-list", async () =>
            {
                var queryable = await _branches.GetQueryableAsync();
                var entities = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.IsActive).OrderBy(x => x.Name));
                return entities.Select(MapToDto).ToList();
            });
        }

        /// <summary>
        /// Admin: xem chi tiết một chi nhánh. Ném exception khi Id không tồn tại.
        /// </summary>
        /// <param name="id">Id chi nhánh.</param>
        /// <returns>Thông tin chi nhánh.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<BranchDto> GetAsync(Guid id)
        {
            return MapToDto(await _branches.GetAsync(id));
        }

        /// <summary>
        /// Admin: tạo chi nhánh mới. Tên/địa chỉ/SĐT được trim khoảng trắng;
        /// địa chỉ/SĐT bỏ trống thì lưu null (không lưu chuỗi rỗng).
        /// </summary>
        /// <param name="dto">Tên, địa chỉ, SĐT, trạng thái hoạt động.</param>
        /// <returns>Chi nhánh vừa tạo.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<BranchDto> CreateAsync(CreateUpdateBranchDto dto)
        {
            var entity = new Branch
            {
                Name = dto.Name.Trim(),
                Address = StringHelpers.TrimToNull(dto.Address),
                Phone = StringHelpers.TrimToNull(dto.Phone),
                IsActive = dto.IsActive
            };
            await _branches.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: cập nhật thông tin chi nhánh (ghi đè toàn bộ trường bằng DTO
        /// mới) và invalidate cache danh sách đang hoạt động.
        /// </summary>
        /// <param name="id">Id chi nhánh cần cập nhật.</param>
        /// <param name="dto">Thông tin mới của chi nhánh.</param>
        /// <returns>Chi nhánh sau khi cập nhật.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<BranchDto> UpdateAsync(Guid id, CreateUpdateBranchDto dto)
        {
            var entity = await _branches.GetAsync(id);
            entity.Name = dto.Name.Trim();
            entity.Address = StringHelpers.TrimToNull(dto.Address);
            entity.Phone = StringHelpers.TrimToNull(dto.Phone);
            entity.IsActive = dto.IsActive;
            await _branches.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: xóa chi nhánh và invalidate cache danh sách đang hoạt động.
        /// </summary>
        /// <param name="id">Id chi nhánh cần xóa.</param>
        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            await _branches.DeleteAsync(id, autoSave: true);
            _catalogCache.Invalidate();
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
