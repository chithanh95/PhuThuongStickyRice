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
    /// <summary>
    /// Hợp đồng quản lý topping (phần ăn thêm cho xôi): shop lấy danh sách
    /// topping đang bật để khách tick chọn; admin CRUD ở màn hình quản lý.
    /// </summary>
    public interface IToppingAppService : IApplicationService
    {
        /// <summary>Public: danh sách topping đang bật cho khách tick chọn khi đặt món.</summary>
        Task<List<ToppingDto>> GetActiveAsync();

        /// <summary>Admin: toàn bộ topping (kể cả đã tắt), sắp xếp theo thứ tự hiển thị.</summary>
        Task<List<ToppingDto>> GetListAsync();

        /// <summary>
        /// Admin: chi tiết 1 topping.
        /// </summary>
        /// <param name="id">Id topping.</param>
        /// <returns>Topping tìm được; throw khi Id không tồn tại.</returns>
        Task<ToppingDto> GetAsync(Guid id);

        /// <summary>
        /// Admin: tạo topping mới (giá phụ thu không được âm).
        /// </summary>
        /// <param name="dto">Tên, giá phụ thu, thứ tự hiển thị, trạng thái bật/tắt.</param>
        /// <returns>Topping vừa tạo.</returns>
        Task<ToppingDto> CreateAsync(CreateUpdateToppingDto dto);

        /// <summary>
        /// Admin: cập nhật topping (ghi đè toàn bộ trường bằng DTO mới).
        /// </summary>
        /// <param name="id">Id topping cần cập nhật.</param>
        /// <param name="dto">Thông tin mới của topping.</param>
        /// <returns>Topping sau khi cập nhật.</returns>
        Task<ToppingDto> UpdateAsync(Guid id, CreateUpdateToppingDto dto);

        /// <summary>
        /// Admin: xóa topping. Tên topping được lưu dạng text trong OrderLine nên
        /// không cần giữ FK, nhưng xóa mềm (tắt trạng thái) để lịch sử đơn cũ
        /// vẫn hiển thị đúng và tránh nhầm nếu topping được tạo lại sau này.
        /// </summary>
        /// <param name="id">Id topping cần tắt.</param>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Quản lý topping cho shop và admin. Danh sách topping đang bật được cache
    /// (<c>IProductCatalogCache</c>) phục vụ checkout của khách, nên mọi thao tác
    /// tạo/sửa/tắt đều phải invalidate cache để khách thấy thay đổi ngay lập tức.
    /// </summary>
    public class ToppingAppService : ApplicationService, IToppingAppService
    {
        private readonly IRepository<Topping, Guid> _toppings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        public ToppingAppService(
            IRepository<Topping, Guid> toppings,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _toppings = toppings;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Public: topping đang bật, sắp xếp theo thứ tự hiển thị rồi theo tên.
        /// Kết quả cache qua <c>IProductCatalogCache</c> vì checkout của khách
        /// gọi rất thường xuyên.
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<List<ToppingDto>> GetActiveAsync()
        {
            return await _catalogCache.GetOrAddAsync("toppings:active", QueryActiveAsync);
        }

        /// <summary>
        /// Truy vấn DB lấy topping đang bán (bản chưa qua cache) —
        /// shop dùng danh sách này để khách chọn thêm topping vào món.
        /// </summary>
        private async Task<List<ToppingDto>> QueryActiveAsync()
        {
            var queryable = await _toppings.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: toàn bộ topping (kể cả đã tắt), sắp xếp theo thứ tự hiển thị rồi theo tên.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<ToppingDto>> GetListAsync()
        {
            var queryable = await _toppings.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: chi tiết 1 topping.
        /// </summary>
        /// <param name="id">Id topping.</param>
        /// <returns>Topping tìm được; ném exception khi Id không tồn tại.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<ToppingDto> GetAsync(Guid id)
        {
            return MapToDto(await _toppings.GetAsync(id));
        }

        /// <summary>
        /// Admin: tạo topping mới. Giá phụ thu bị kẹp ≥ 0 (giá âm sẽ làm giảm
        /// tiền đơn hàng nên không cho phép); tạo xong invalidate cache topping
        /// đang bật để shop thấy ngay.
        /// </summary>
        /// <param name="dto">Tên, giá phụ thu, thứ tự hiển thị, trạng thái bật/tắt.</param>
        /// <returns>Topping vừa tạo.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<ToppingDto> CreateAsync(CreateUpdateToppingDto dto)
        {
            var entity = new Topping
            {
                Name = dto.Name.Trim(),
                ExtraPrice = Math.Max(0, dto.ExtraPrice),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            await _toppings.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: cập nhật topping (ghi đè toàn bộ trường bằng DTO mới, giá phụ
        /// thu kẹp ≥ 0) và invalidate cache topping đang bật.
        /// </summary>
        /// <param name="id">Id topping cần cập nhật.</param>
        /// <param name="dto">Thông tin mới của topping.</param>
        /// <returns>Topping sau khi cập nhật.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<ToppingDto> UpdateAsync(Guid id, CreateUpdateToppingDto dto)
        {
            var entity = await _toppings.GetAsync(id);

            entity.Name = dto.Name.Trim();
            entity.ExtraPrice = Math.Max(0, dto.ExtraPrice);
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;

            await _toppings.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: "xóa" topping bằng cách tắt trạng thái (soft-delete) thay vì
        /// xóa cứng — tên topping đã lưu dạng text trong OrderLine của các đơn
        /// cũ nên xóa cứng sẽ không ảnh hưởng đơn cũ, nhưng tắt trạng thái giữ
        /// danh sách gọn và tránh nhầm lẫn nếu topping được tạo lại sau này.
        /// </summary>
        /// <param name="id">Id topping cần tắt.</param>
        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            // Topping lưu tên trong OrderLine.ToppingNames (string, không FK),
            // nhưng để giữ lịch sử đơn hàng hiển thị đúng thì deactivate
            // thay vì xóa cứng.
            var entity = await _toppings.GetAsync(id);
            entity.IsActive = false;
            await _toppings.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
        }

        /// <summary>Chuyển entity <c>Topping</c> sang DTO trả về cho client.</summary>
        private static ToppingDto MapToDto(Topping entity)
        {
            return new ToppingDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ExtraPrice = entity.ExtraPrice,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive
            };
        }
    }
}
