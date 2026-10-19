
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Orders;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Item 82: quản lý danh sách shipper giao hàng (Admin).
    /// Shipper ở đây là người giao hàng của quán (không phải đơn vị vận chuyển),
    /// dùng để gán đơn khi giao và theo dõi trên màn hình vận hành.
    /// </summary>
    public interface IShipperAppService : IApplicationService
    {
        /// <summary>Danh sách shipper, sắp xếp theo tên. activeOnly=true chỉ lấy shipper đang hoạt động.</summary>
        /// <param name="activeOnly">true: chỉ lấy shipper đang hoạt động (mặc định lấy tất cả).</param>
        Task<List<ShipperDto>> GetListAsync(bool activeOnly = false);

        /// <summary>Tạo shipper mới (mặc định trạng thái đang hoạt động).</summary>
        /// <param name="input">Tên (bắt buộc) và SĐT (tùy chọn).</param>
        Task<ShipperDto> CreateAsync(CreateShipperDto input);

        /// <summary>Cập nhật tên, SĐT, trạng thái hoạt động của shipper.</summary>
        /// <param name="id">Id shipper.</param>
        /// <param name="input">Thông tin cập nhật.</param>
        Task<ShipperDto> UpdateAsync(Guid id, UpdateShipperDto input);

        /// <summary>Xóa shipper (chỉ xóa khi không còn đơn nào gắn shipper này).</summary>
        /// <param name="id">Id shipper.</param>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Item 82: CRUD danh sách shipper giao hàng cho admin.
    /// CRUD đơn giản trên bảng Shipper — các đơn hàng chỉ lưu tham chiếu
    /// tới shipper nên xóa shipper đang có đơn gắn vào sẽ bị chặn ở tầng DB.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ShipperAppService : ApplicationService, IShipperAppService
    {
        private readonly IRepository<Shipper, Guid> _shippers;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo service quản lý shipper.</summary>
        public ShipperAppService(
            IRepository<Shipper, Guid> shippers,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _shippers = shippers;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Lấy danh sách shipper sắp xếp theo tên A-Z.
        /// </summary>
        /// <param name="activeOnly">true: chỉ lấy shipper đang hoạt động (dùng khi gán đơn giao).</param>
        /// <returns>Danh sách shipper.</returns>
        public virtual async Task<List<ShipperDto>> GetListAsync(bool activeOnly = false)
        {
            var queryable = await _shippers.GetQueryableAsync();
            System.Linq.IQueryable<Shipper> query = queryable.OrderBy(x => x.Name);
            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await _asyncExecuter.ToListAsync(query);
            return items.Select(Map).ToList();
        }

        /// <summary>
        /// Tạo shipper mới: chuẩn hóa tên/SĐT (trim), SĐT trống lưu null,
        /// trạng thái mặc định là đang hoạt động.
        /// </summary>
        /// <param name="input">Tên (bắt buộc) và SĐT (tùy chọn).</param>
        /// <returns>Shipper vừa tạo.</returns>
        public virtual async Task<ShipperDto> CreateAsync(CreateShipperDto input)
        {
            var shipper = new Shipper
            {
                Name = input.Name.Trim(),
                Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _shippers.InsertAsync(shipper, autoSave: true);
            return Map(shipper);
        }

        /// <summary>
        /// Cập nhật thông tin shipper: tên, SĐT (trống → null), trạng thái hoạt động.
        /// </summary>
        /// <param name="id">Id shipper.</param>
        /// <param name="input">Thông tin cập nhật.</param>
        /// <returns>Shipper sau cập nhật.</returns>
        public virtual async Task<ShipperDto> UpdateAsync(Guid id, UpdateShipperDto input)
        {
            var shipper = await _shippers.GetAsync(id);
            shipper.Name = input.Name.Trim();
            shipper.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            shipper.IsActive = input.IsActive;
            await _shippers.UpdateAsync(shipper, autoSave: true);
            return Map(shipper);
        }

        /// <summary>
        /// Xóa shipper theo Id. Nếu shipper đang được đơn hàng tham chiếu,
        /// DB sẽ chặn xóa (throw) thay vì xóa mềm — xóa hộ được nhưng phải
        /// gỡ gán đơn trước trên màn hình vận hành.
        /// </summary>
        /// <param name="id">Id shipper cần xóa.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _shippers.DeleteAsync(id, autoSave: true);
        }

        private static ShipperDto Map(Shipper s) => new ShipperDto
        {
            Id = s.Id,
            Name = s.Name,
            Phone = s.Phone,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}
