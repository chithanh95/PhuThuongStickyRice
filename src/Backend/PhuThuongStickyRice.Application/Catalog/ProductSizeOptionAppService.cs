using PhuThuongStickyRice.Application.Products;
using PhuThuongStickyRice.Application.Products.DTOs;
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
    /// Quản lý các tùy chọn size (Nhỏ/Vừa/Lớn...) gắn vào từng sản phẩm.
    /// Giá size hiển thị trên shop nên mọi thay đổi đều xóa cache catalog.
    /// </summary>
    public interface IProductSizeOptionAppService : IApplicationService
    {
        /// <summary>
        /// Lấy toàn bộ size của 1 sản phẩm, sắp xếp theo thứ tự hiển thị
        /// rồi đến giá tăng dần.
        /// </summary>
        /// <param name="productId">Id sản phẩm.</param>
        /// <returns>Danh sách size (kể cả size đã tắt, để admin quản lý).</returns>
        Task<List<ProductSizeOptionDto>> GetByProductAsync(Guid productId);

        /// <summary>
        /// Tạo size mới cho sản phẩm. Tên được trim, giá âm bị ép về 0.
        /// </summary>
        /// <param name="dto">Tên, giá, thứ tự hiển thị, trạng thái bật/tắt.</param>
        /// <returns>Size vừa tạo.</returns>
        Task<ProductSizeOptionDto> CreateAsync(CreateUpdateProductSizeOptionDto dto);

        /// <summary>Cập nhật tên/giá/thứ tự/trạng thái của 1 size.</summary>
        /// <param name="id">Id size cần sửa.</param>
        /// <param name="dto">Dữ liệu mới.</param>
        /// <returns>Size sau khi cập nhật.</returns>
        Task<ProductSizeOptionDto> UpdateAsync(Guid id, CreateUpdateProductSizeOptionDto dto);

        /// <summary>
        /// Xóa size. Không dùng cho size đã phát sinh trong đơn cũ —
        /// với size đó chỉ nên tắt (IsActive = false) để giữ lịch sử đơn.
        /// </summary>
        /// <param name="id">Id size cần xóa.</param>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Service CRUD tùy chọn size sản phẩm (admin). Sau mỗi lần thêm/sửa/xóa
    /// đều xóa cache catalog vì giá size hiển thị trực tiếp trên shop.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ProductSizeOptionAppService : ApplicationService, IProductSizeOptionAppService
    {
        private readonly IRepository<ProductSizeOption, Guid> _options;
        private readonly IRepository<Product, Guid> _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IProductCatalogCache _catalogCache;

        /// <summary>
        /// Khởi tạo service quản lý size sản phẩm.
        /// </summary>
        /// <param name="options">Repository tùy chọn size.</param>
        /// <param name="products">Repository sản phẩm (để kiểm tra sản phẩm tồn tại).</param>
        /// <param name="asyncExecuter">Chạy query async trên mọi EF provider.</param>
        /// <param name="catalogCache">Cache catalog public của shop.</param>
        public ProductSizeOptionAppService(
            IRepository<ProductSizeOption, Guid> options,
            IRepository<Product, Guid> products,
            IAsyncQueryableExecuter asyncExecuter,
            IProductCatalogCache catalogCache)
        {
            _options = options;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Lấy danh sách size của 1 sản phẩm (xem mô tả ở interface).
        /// </summary>
        public virtual async Task<List<ProductSizeOptionDto>> GetByProductAsync(Guid productId)
        {
            var queryable = await _options.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.ProductId == productId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Price));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Tạo size mới cho sản phẩm (xem mô tả ở interface).
        /// </summary>
        public virtual async Task<ProductSizeOptionDto> CreateAsync(CreateUpdateProductSizeOptionDto dto)
        {
            await EnsureProductExistsAsync(dto.ProductId);

            var entity = new ProductSizeOption
            {
                ProductId = dto.ProductId,
                Name = dto.Name.Trim(),
                // Giá âm không có nghĩa nghiệp vụ (có thể do nhập tay ở form
                // admin) nên ép về 0 thay vì ném lỗi — đơn cũ vẫn tính đúng.
                Price = Math.Max(0, dto.Price),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            await _options.InsertAsync(entity, autoSave: true);
            // Giá size hiện trực tiếp trên shop (đọc từ cache catalog) nên
            // phải xóa cache ngay, nếu không khách vẫn thấy giá cũ.
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Cập nhật 1 size (xem mô tả ở interface).
        /// </summary>
        public virtual async Task<ProductSizeOptionDto> UpdateAsync(Guid id, CreateUpdateProductSizeOptionDto dto)
        {
            var entity = await _options.GetAsync(id);

            entity.Name = dto.Name.Trim();
            // Giống CreateAsync: giá âm ép về 0 thay vì ném lỗi.
            entity.Price = Math.Max(0, dto.Price);
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;

            await _options.UpdateAsync(entity, autoSave: true);
            // Giá đổi phải xóa cache catalog ngay (xem CreateAsync).
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Xóa 1 size (xem lưu ý ở interface).
        /// </summary>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _options.DeleteAsync(id);
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Ném lỗi khi tạo size cho sản phẩm không tồn tại — chặn size mồ côi
        /// (form admin có thể gửi productId đã bị xóa ở tab khác).
        /// </summary>
        private async Task EnsureProductExistsAsync(Guid productId)
        {
            if (await _products.FindAsync(productId) == null)
            {
                throw new Domain.Exceptions.DomainException($"Product '{productId}' not found.");
            }
        }

        /// <summary>Chuyển entity <c>ProductSizeOption</c> sang DTO trả về cho client.</summary>
        private static ProductSizeOptionDto MapToDto(ProductSizeOption entity)
        {
            return new ProductSizeOptionDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                Name = entity.Name,
                Price = entity.Price,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive
            };
        }
    }
}
