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
    /// Hợp đồng quản lý combo món ăn (gói nhiều món bán chung 1 giá).
    /// </summary>
    public interface IComboAppService : IApplicationService
    {
        /// <summary>Public: active combos for the shop.</summary>
        Task<List<ComboDto>> GetActiveAsync();

        /// <summary>Admin: toàn bộ combo (kể cả đã tắt), mới nhất trước.</summary>
        Task<List<ComboDto>> GetListAsync();

        /// <summary>Admin: chi tiết 1 combo.</summary>
        /// <param name="id">Id combo.</param>
        /// <returns>Combo kèm danh sách món trong combo.</returns>
        Task<ComboDto> GetAsync(Guid id);

        /// <summary>
        /// Admin: tạo combo mới. Giá combo không được âm; các món trong combo
        /// phải là sản phẩm đang tồn tại.
        /// </summary>
        /// <param name="dto">Tên, giá, trạng thái và danh sách món của combo.</param>
        /// <returns>Combo vừa tạo.</returns>
        Task<ComboDto> CreateAsync(CreateUpdateComboDto dto);

        /// <summary>
        /// Admin: cập nhật combo (thay toàn bộ danh sách món bằng danh sách mới).
        /// </summary>
        /// <param name="id">Id combo cần cập nhật.</param>
        /// <param name="dto">Thông tin combo mới.</param>
        /// <returns>Combo sau cập nhật.</returns>
        Task<ComboDto> UpdateAsync(Guid id, CreateUpdateComboDto dto);

        /// <summary>
        /// Admin: xóa combo. Combo đã từng xuất hiện trong đơn hàng thì chỉ
        /// tắt (Ngưng bán) để không làm gãy lịch sử đơn, chưa từng bán mới xóa cứng.
        /// </summary>
        /// <param name="id">Id combo cần xóa.</param>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Quản lý combo món ăn cho shop và admin. Combo shop lấy từ cache
    /// (product catalog cache) nên mọi thay đổi phải invalidate cache.
    /// </summary>
    public class ComboAppService : ApplicationService, IComboAppService
    {
        private readonly IRepository<Combo, Guid> _combos;
        private readonly IRepository<Product, Guid> _products;
        private readonly IRepository<OrderLine, Guid> _orderLines;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        /// <summary>Khởi tạo service combo với repository combo/sản phẩm/dòng đơn và cache catalog.</summary>
        public ComboAppService(
            IRepository<Combo, Guid> combos,
            IRepository<Product, Guid> products,
            IRepository<OrderLine, Guid> orderLines,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _combos = combos;
            _products = products;
            _orderLines = orderLines;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Public (không cần đăng nhập): danh sách combo đang bán cho shop,
        /// lấy từ cache catalog. Combo có món đã ngừng bán sẽ bị ẩn ở shop.
        /// </summary>
        /// <returns>Combo đang bán, giá tăng dần.</returns>
        [AllowAnonymous]
        public virtual async Task<List<ComboDto>> GetActiveAsync()
        {
            return await _catalogCache.GetOrAddAsync("combos:active", QueryActiveAsync);
        }

        /// <summary>
        /// Truy vấn combo đang bán từ DB (dùng làm factory cho cache).
        /// Ẩn combo không còn món hợp lệ nào để shop không bán combo trống.
        /// </summary>
        private async Task<List<ComboDto>> QueryActiveAsync()
        {
            var queryable = await _combos.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive).OrderBy(x => x.Price));
            return await MapToDtosAsync(entities, includeInvalidItems: false);
        }

        /// <summary>
        /// Admin: toàn bộ combo (kể cả đã tắt), mới nhất trước.
        /// </summary>
        /// <returns>Danh sách combo kèm tên/giá món trong combo.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<ComboDto>> GetListAsync()
        {
            var queryable = await _combos.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedAt));
            return await MapToDtosAsync(entities);
        }

        /// <summary>Admin: chi tiết 1 combo.</summary>
        /// <param name="id">Id combo.</param>
        /// <returns>Combo kèm danh sách món.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<ComboDto> GetAsync(Guid id)
        {
            var entity = await _combos.GetAsync(id);
            return (await MapToDtosAsync(new[] { entity })).First();
        }

        /// <summary>
        /// Admin: tạo combo mới. Giá combo ép về không âm; validate món tồn tại
        /// trước khi lưu; xong thì invalidate cache catalog để shop thấy ngay.
        /// </summary>
        /// <param name="dto">Tên, giá, trạng thái và danh sách món của combo.</param>
        /// <returns>Combo vừa tạo.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<ComboDto> CreateAsync(CreateUpdateComboDto dto)
        {
            await EnsureItemsValidAsync(dto);

            var entity = new Combo
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Price = Math.Max(0, dto.Price),
                IsActive = dto.IsActive
            };

            foreach (var item in dto.Items ?? Enumerable.Empty<CreateUpdateComboItemDto>())
            {
                entity.Items.Add(new ComboItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await _combos.InsertAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return (await MapToDtosAsync(new[] { entity })).First();
        }

        /// <summary>
        /// Admin: cập nhật combo — thay toàn bộ danh sách món bằng danh sách mới
        /// (xóa items cũ rồi thêm lại) để tránh rắc rối đối chiếu từng dòng.
        /// </summary>
        /// <param name="id">Id combo cần cập nhật.</param>
        /// <param name="dto">Thông tin combo mới.</param>
        /// <returns>Combo sau cập nhật.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<ComboDto> UpdateAsync(Guid id, CreateUpdateComboDto dto)
        {
            await EnsureItemsValidAsync(dto);

            var entity = await _combos.GetAsync(id);

            entity.Name = dto.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            entity.Price = Math.Max(0, dto.Price);
            entity.IsActive = dto.IsActive;

            entity.Items.Clear();
            foreach (var item in dto.Items ?? Enumerable.Empty<CreateUpdateComboItemDto>())
            {
                entity.Items.Add(new ComboItem
                {
                    ComboId = entity.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await _combos.UpdateAsync(entity, autoSave: true);
            _catalogCache.Invalidate();
            return (await MapToDtosAsync(new[] { entity })).First();
        }

        /// <summary>
        /// Admin: xóa combo. Combo đã từng xuất hiện trong đơn hàng thì chỉ tắt
        /// (Ngưng bán) để không làm gãy lịch sử đơn, chưa từng bán mới xóa cứng.
        /// </summary>
        /// <param name="id">Id combo cần xóa.</param>
        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            // OrderLine.ProductId chứa combo id khi IsCombo=true.
            // Nếu combo đã có trong đơn hàng thì deactivate thay vì xóa cứng.
            var orderLinesQuery = await _orderLines.GetQueryableAsync();
            var hasOrderHistory = await _asyncExecuter.AnyAsync(
                orderLinesQuery.Where(x => x.ProductId == id && x.IsCombo));
            var entity = await _combos.GetAsync(id);
            if (hasOrderHistory)
            {
                entity.IsActive = false;
                await _combos.UpdateAsync(entity, autoSave: true);
            }
            else
            {
                await _combos.DeleteAsync(entity, autoSave: true);
            }
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Kiểm tra danh sách món của combo: phải có ít nhất 1 món và mọi
        /// ProductId đều trỏ tới sản phẩm đang tồn tại (tránh combo chứa món ảo).
        /// </summary>
        /// <param name="dto">Dữ liệu combo cần kiểm tra.</param>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi combo rỗng hoặc chứa sản phẩm không tồn tại.</exception>
        private async Task EnsureItemsValidAsync(CreateUpdateComboDto dto)
        {
            var items = dto.Items ?? new List<CreateUpdateComboItemDto>();
            if (items.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Combo phải có ít nhất một món.");
            }

            var productIds = items.Select(x => x.ProductId).Distinct().ToList();
            var queryable = await _products.GetQueryableAsync();
            var count = await _asyncExecuter.LongCountAsync(
                queryable.Where(x => productIds.Contains(x.Id)));
            if (count != productIds.Count)
            {
                throw new Domain.Exceptions.DomainException("Combo chứa sản phẩm không tồn tại.");
            }
        }

        /// <summary>
        /// Map combo sang DTO, gắn tên/giá hiện tại của từng món (1 query batch).
        /// Món đã ngừng bán hiện tên "(Món đã ngừng bán)", giá 0 — admin vẫn thấy,
        /// còn shop thì combo sẽ bị ẩn nếu không còn món hợp lệ nào.
        /// </summary>
        /// <param name="entities">Các combo cần map.</param>
        /// <param name="includeInvalidItems">
        /// false (luồng shop): loại món không hợp lệ và ẩn combo trống;
        /// true (luồng admin): giữ nguyên để admin thấy và sửa.
        /// </param>
        /// <returns>Danh sách ComboDto.</returns>
        private async Task<List<ComboDto>> MapToDtosAsync(IEnumerable<Combo> entities, bool includeInvalidItems = true)
        {
            var list = entities.ToList();
            var productIds = list.SelectMany(x => x.Items).Select(x => x.ProductId).Distinct().ToList();

            var names = await LoadProductInfoMapAsync(productIds);

            var dtos = list.Select(e => new ComboDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Price = e.Price,
                IsActive = e.IsActive,
                Items = e.Items
                    .Where(i => includeInvalidItems || names.ContainsKey(i.ProductId))
                    .Select(i =>
                    {
                        // Chỉ lookup dictionary 1 lần/món (tránh 2 lần băm
                        // trùng lặp cho Name và Price).
                        var hasProduct = names.TryGetValue(i.ProductId, out var productInfo);
                        return new ComboItemDto
                        {
                            ProductId = i.ProductId,
                            ProductName = hasProduct ? productInfo.Name : "(Món đã ngừng bán)",
                            ProductPrice = hasProduct ? productInfo.Price : 0,
                            Quantity = i.Quantity
                        };
                    }).ToList()
            });

            // Shop: ẩn combo không còn món hợp lệ nào (VD: món bị xóa ở admin).
            if (!includeInvalidItems)
            {
                dtos = dtos.Where(d => d.Items.Count > 0);
            }

            return dtos.ToList();
        }

        /// <summary>
        /// Nạp (tên, giá) các món trong combo theo batch để map DTO.
        /// </summary>
        /// <remarks>
        /// perf-hunt 2026-10-19 (run 18:05): chỉ dùng Name + Price nên
        /// project 3 cột trong SQL, không nạp full entity Product
        /// (description, JSON, owned Money VOs, audit fields).
        /// </remarks>
        private async Task<Dictionary<Guid, (string Name, decimal Price)>> LoadProductInfoMapAsync(
            List<Guid> productIds)
        {
            var names = new Dictionary<Guid, (string Name, decimal Price)>();
            if (productIds.Count == 0)
            {
                return names;
            }

            var queryable = await _products.GetQueryableAsync();
            var rows = await _asyncExecuter.ToListAsync(
                queryable.Where(x => productIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.Name, Price = x.Price.Amount }));
            foreach (var row in rows)
            {
                names[row.Id] = (row.Name, row.Price);
            }

            return names;
        }
    }
}
