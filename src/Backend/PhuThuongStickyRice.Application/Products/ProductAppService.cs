using PhuThuongStickyRice.Application.Products.DTOs;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using Volo.Abp.Uow;
using Volo.Abp.ObjectMapping;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Products
{
    /// <summary>
    /// Hợp đồng dịch vụ catalog sản phẩm: danh sách phân trang (có đường
    /// public che giá vốn), CRUD món cho admin, bật/tắt trạng thái bán,
    /// thứ tự hiển thị thủ công, nhật ký thay đổi, suất còn trong ngày và
    /// tổng hợp đánh giá món cho shop.
    /// </summary>
    public interface IProductAppService : IApplicationService
    {
        /// <summary>
        /// Danh sách món có phân trang. Đường public (status=Active) đọc từ
        /// cache 5 phút; đường admin (không lọc status) luôn đọc DB để thấy
        /// dữ liệu mới nhất ngay sau khi sửa.
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm, lọc chi nhánh/trạng thái, phân trang.</param>
        /// <returns>Trang sản phẩm hiện tại.</returns>
        Task<PagedResultDto<ProductDto>> GetListAsync(GetProductsInput input);

        /// <summary>
        /// Chi tiết 1 món theo Id (đường admin — có cả giá vốn).
        /// </summary>
        /// <param name="id">Id của món.</param>
        /// <returns>Thông tin món.</returns>
        Task<ProductDto> GetAsync(Guid id);

        /// <summary>
        /// bug-hunt 2026-10-19: danh sách món cho kênh public — giống
        /// <see cref="GetListAsync"/> nhưng che giá vốn (CostPrice, thông tin
        /// nội bộ) khỏi người không phải Admin. Luôn clone DTO trước khi mask
        /// để không làm bẩn cache catalog dùng chung.
        /// </summary>
        Task<PagedResultDto<ProductDto>> GetPublicListAsync(GetProductsInput input);

        /// <summary>
        /// bug-hunt 2026-10-19: chi tiết món cho kênh public — giống
        /// <see cref="GetAsync"/> nhưng che giá vốn khỏi người không phải Admin.
        /// </summary>
        Task<ProductDto> GetPublicAsync(Guid id);

        /// <summary>
        /// Admin: tạo món mới (tên, mã, giá bán, giá vốn, ảnh, danh mục...).
        /// Cần quyền products.edit; ghi nhật ký hoạt động admin.
        /// </summary>
        Task<ProductDto> CreateAsync(CreateUpdateProductDto input);

        /// <summary>
        /// Admin: cập nhật món (giá bán/giá KM, trạng thái, khung giờ bán,
        /// ảnh, chi nhánh...). Mỗi field thay đổi được ghi vào nhật ký
        /// thay đổi (item 148). Cần quyền products.edit.
        /// </summary>
        Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input);

        /// <summary>
        /// Admin: xóa món khỏi catalog. Cần quyền products.delete; ghi nhật
        /// ký hoạt động admin.
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Bật/tắt nhanh trạng thái hết hàng: Đang bán -> Hết hàng -> Đang bán.
        /// Món đã "Ngừng bán" không được bật/tắt. Cần quyền products.edit.
        /// </summary>
        Task<ProductDto> ToggleAvailabilityAsync(Guid id);

        /// <summary>
        /// Item 146: lưu thứ tự hiển thị thủ công (kéo-thả ở trang /products).
        /// Danh sách Id đã theo đúng thứ tự hiển thị; mỗi món nhận
        /// SortOrder = vị trí (index) của nó trong danh sách. Cần quyền products.edit.
        /// </summary>
        Task SaveDisplayOrderAsync(List<Guid> productIds);

        /// <summary>
        /// Item 148: lịch sử thay đổi của món ("ai sửa gì, khi nào"),
        /// mới nhất lên trước. Chỉ admin.
        /// </summary>
        Task<List<ProductChangeLogDto>> GetChangeLogAsync(Guid productId);

        /// <summary>
        /// Item 165: trạng thái giới hạn suất/ngày của món cho shop public
        /// ("Còn N suất"). Public, không cần đăng nhập.
        /// </summary>
        Task<List<ProductDailyRemainingDto>> GetDailyRemainingAsync();

        /// <summary>
        /// Wave 6b (item 26): tổng hợp đánh giá từng món cho kênh public từ
        /// review đơn hàng không bị ẩn. Món chưa có review bị bỏ qua.
        /// </summary>
        Task<List<ProductRatingDto>> GetProductRatingsAsync();
    }

    /// <summary>
    /// AppService quản lý catalog sản phẩm: danh sách phân trang, CRUD món,
    /// bật/tắt trạng thái bán. Đường public (shop) dùng cache 5 phút, đường admin
    /// luôn đọc DB. Mọi thao tác thêm/sửa/xóa cần quyền products.edit/products.delete
    /// và được ghi vào nhật ký hoạt động nhân viên.
    /// </summary>
    public class ProductAppService : ApplicationService, IProductAppService
    {
        private const string DailyRemainingCacheKey = "daily-remaining";
        /// <summary>
        /// Cache "Còn N suất" chỉ 60 giây vì số suất còn lại thay đổi theo
        /// từng đơn hàng mới — cache lâu hơn sẽ hiển thị số liệu cũ.
        /// </summary>
        private const int DailyRemainingCacheSeconds = 60;

        // perf-hunt 2026-10-19: trần kích thước 1 trang danh sách sản phẩm —
        // GetProductsInput không có [Range] nên client có thể gửi
        // MaxResultCount=int.MaxValue, gây query DB nặng + phình cache
        // (key public chứa nguyên giá trị skip/max do client truyền).
        private const int MaxListPageSize = 200;

        // Log ghi 1 dòng mỗi field đổi mỗi lần sửa — bảng chỉ tăng, không
        // bao giờ nạp hết; xem lịch sử 1 món chỉ cần 200 dòng mới nhất.
        private const int MaxChangeLogRows = 200;

        // Độ dài tối đa của mô tả món trong snapshot nhật ký thay đổi
        // (item 148): mô tả dài không cần lưu hết, 200 ký tự đủ tra cứu.
        private const int SnapshotDescriptionMaxLength = 200;

        private readonly IProductRepository _products;
        private readonly IDomainEvents _domainEvents;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;
        private readonly Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> _sizeOptions;
        private readonly IProductCatalogCache _catalogCache;
        private readonly Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> _reviews;
        private readonly Volo.Abp.Domain.Repositories.IRepository<ProductChangeLog, Guid> _changeLogs;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Order, Guid> _orders;
        private readonly Volo.Abp.Domain.Repositories.IRepository<OrderLine, Guid> _orderLines;
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Khởi tạo service sản phẩm: repository món, domain event, cache
        /// catalog (danh sách món/đánh giá/KM), cache memory (suất còn lại),
        /// kiểm tra quyền và ghi nhật ký hoạt động admin.
        /// </summary>
        public ProductAppService(
            IProductRepository products,
            IDomainEvents domainEvents,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper,
            Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> sizeOptions,
            IProductCatalogCache catalogCache,
            Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> reviews,
            Volo.Abp.Domain.Repositories.IRepository<ProductChangeLog, Guid> changeLogs,
            Volo.Abp.Domain.Repositories.IRepository<Order, Guid> orders,
            Volo.Abp.Domain.Repositories.IRepository<OrderLine, Guid> orderLines,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger,
            IMemoryCache memoryCache)
        {
            _products = products;
            _domainEvents = domainEvents;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
            _sizeOptions = sizeOptions;
            _catalogCache = catalogCache;
            _reviews = reviews;
            _changeLogs = changeLogs;
            _orders = orders;
            _orderLines = orderLines;
            _permissions = permissions;
            _activityLogger = activityLogger;
            _memoryCache = memoryCache;
        }

        [UnitOfWork]
        /// <summary>
        /// Lấy danh sách món có phân trang. Đường public (status=Active) đọc từ cache
        /// 5 phút để chịu tải trang shop; đường admin (không lọc status) luôn đọc DB
        /// để thấy dữ liệu mới nhất ngay sau khi sửa.
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm, lọc chi nhánh/trạng thái, phân trang.</param>
        /// <returns>Trang sản phẩm hiện tại.</returns>
        public virtual async Task<PagedResultDto<ProductDto>> GetListAsync(GetProductsInput input)
        {
            // perf-hunt 2026-10-19: clamp phân trang ngay ở entry point.
            // GetProductsInput không có [Range] nên client có thể gửi
            // MaxResultCount=int.MaxValue -> query DB nặng + phình cache
            // (key public chứa nguyên giá trị skip/max do client truyền).
            // Shop chỉ có vài chục món, admin dùng 10/trang -> clamp 200 an toàn.
            input ??= new GetProductsInput();
            input.SkipCount = Math.Max(0, input.SkipCount);
            input.MaxResultCount = Math.Clamp(input.MaxResultCount, 1, MaxListPageSize);

            // Item 101: the public shop path (status=Active) is served from a
            // 5-minute in-memory cache. The admin list (no status filter)
            // always hits the database.
            var isPublicPath = string.Equals(input.Status, "Active", StringComparison.OrdinalIgnoreCase);
            if (!isPublicPath)
            {
                return await QueryListAsync(input);
            }

            // perf-hunt 2026-10-19: KHÔNG cache khi có SearchTerm — cache key
            // chứa nguyên từ khóa do user nhập (đã trim/lowercase), bot quét
            // term lạ sẽ phình cache (mỗi term 1 entry tới 200 DTO, TTL 5').
            // Kết quả tìm kiếm giống hệt vì cùng QueryListAsync, chỉ không qua
            // cache. Path nóng (shop load ban đầu, không term) vẫn cache 5'.
            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                return await QueryListAsync(input);
            }

            var cacheKey = "products:active:"
                + (input.BranchId?.ToString() ?? "all") + ":"
                + (input.Status ?? "all") + ":"
                + (input.IncludeInactive ? "withInactive" : "activeOnly") + ":"
                + input.SkipCount + ":" + input.MaxResultCount;
            return await _catalogCache.GetOrAddAsync(cacheKey, () => QueryListAsync(input));
        }

        /// <summary>
        /// Truy vấn danh sách món có phân trang từ DB (bản chưa qua cache):
        /// sắp xếp theo thứ tự hiển thị thủ công rồi tên, áp bộ lọc
        /// tìm kiếm/trạng thái/chi nhánh, đếm tổng và lấy đúng 1 trang,
        /// gắn size option cho từng món.
        /// </summary>
        private async Task<PagedResultDto<ProductDto>> QueryListAsync(GetProductsInput input)
        {
            var queryable = await _products.GetQueryableAsync();
            // Item 146: the manual display order (drag & drop on /products)
            // comes first; name is the tiebreak. The public shop goes
            // through this same query with status=Active.
            System.Linq.IQueryable<Product> query = queryable
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name);

            query = ApplySearchFilter(query, input);
            query = ApplyStatusFilter(query, input);
            query = ApplyBranchFilter(query, input);

            var totalCount = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

            var dtos = _objectMapper.Map<List<Product>, List<ProductDto>>(items);
            // Restored 2026-10-17: isolation concluded the CRUD failures were
            // caused by the empty-GUID root cause (fixed in Repository.AddAsync),
            // not by AttachSizesAsync — size options are needed for the shop list.
            await AttachSizesAsync(dtos);

            return new PagedResultDto<ProductDto>(totalCount, dtos);
        }

        /// <summary>Lọc sản phẩm theo từ khóa (tên hoặc mã).</summary>
        private static System.Linq.IQueryable<Product> ApplySearchFilter(
            System.Linq.IQueryable<Product> query, GetProductsInput input)
        {
            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x => x.Name.Contains(term) || x.Code.Value.Contains(term));
            }

            return query;
        }

        /// <summary>
        /// Lọc theo trạng thái; shop công khai (status=Active) vẫn thấy món
        /// "Tạm hết" (Inactive) nhưng ẩn món theo mùa ngoài thời gian bán.
        /// </summary>
        private static System.Linq.IQueryable<Product> ApplyStatusFilter(
            System.Linq.IQueryable<Product> query, GetProductsInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Status)
                || !Enum.TryParse<ProductStatus>(input.Status, ignoreCase: true, out var status))
            {
                return query;
            }

            // Item 85: the public shop asks for status=Active with
            // includeInactive=true so sold-out products can be shown
            // with a "Tạm hết" badge. Discontinued stays hidden.
            if (status == ProductStatus.Active && input.IncludeInactive)
            {
                query = query.Where(x =>
                    x.Status == ProductStatus.Active || x.Status == ProductStatus.Inactive);
            }
            else
            {
                query = query.Where(x => x.Status == status);
            }

            // The public shop asks for status=Active: hide seasonal
            // products that are outside their availability window.
            // The admin list (no status filter) still sees them all.
            // Sold-out (Inactive) products are shown regardless of the
            // seasonal window — the "Tạm hết" badge is what matters.
            if (status == ProductStatus.Active)
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);
                // perf-hunt 2026-10-19: so sánh range thuần túy thay vì gọi
                // .Date trên cột (EF dịch .Date thành CONVERT(date, col) →
                // non-sargable, index trên AvailableFrom/AvailableTo không
                // seek được). Tương đương logic chính xác từng dòng:
                // d.Date <= today ⟺ d < tomorrow;
                // d.Date >= today ⟺ d >= today.
                query = query.Where(x =>
                    x.Status == ProductStatus.Inactive
                    || ((x.AvailableFrom == null || x.AvailableFrom < tomorrow)
                        && (x.AvailableTo == null || x.AvailableTo >= today)));
            }

            return query;
        }

        // Item 109: branch filter — products of the branch plus
        // branch-less (global) products.
        /// <summary>Lọc theo chi nhánh (kèm món dùng chung toàn hệ thống).</summary>
        private static System.Linq.IQueryable<Product> ApplyBranchFilter(
            System.Linq.IQueryable<Product> query, GetProductsInput input)
        {
            if (input.BranchId.HasValue)
            {
                var branchId = input.BranchId.Value;
                query = query.Where(x => x.BranchId == null || x.BranchId == branchId);
            }

            return query;
        }

        /// <summary>
        /// Lấy chi tiết một món kèm các lựa chọn size đã cấu hình.
        /// </summary>
        /// <param name="id">Id món.</param>
        /// <returns>Chi tiết món.</returns>
        /// <remarks>
        /// perf-hunt 2026-10-19: cache theo id (TTL 5 phút, key versioned nên
        /// mọi write path gọi <c>Invalidate()</c> đều bust cache này) vì trang
        /// chi tiết món public gọi mỗi lượt xem.
        /// </remarks>
        public virtual async Task<ProductDto> GetAsync(Guid id)
        {
            var cacheKey = $"product:by-id:{id}";
            return await _catalogCache.GetOrAddAsync(cacheKey, async () =>
            {
                var product = await FindOrThrowAsync(id);
                var dto = _objectMapper.Map<Product, ProductDto>(product);
                await AttachSizesAsync(new[] { dto });
                return dto;
            });
        }

        /// <summary>
        /// bug-hunt 2026-10-19: danh sách món cho kênh public — che giá vốn
        /// (CostPrice, thông tin nội bộ) khỏi người không phải Admin. Clone
        /// DTO trước khi mask để không làm bẩn cache catalog dùng chung
        /// (admin gọi cùng cache key vẫn thấy giá vốn đầy đủ).
        /// </summary>
        /// <param name="input">Điều kiện lọc/phân trang.</param>
        /// <returns>Danh sách món đã che giá vốn (trừ Admin).</returns>
        public virtual async Task<PagedResultDto<ProductDto>> GetPublicListAsync(GetProductsInput input)
        {
            var paged = await GetListAsync(input);
            if (paged.Items == null || CurrentUser?.IsInRole("Admin") == true)
            {
                return paged;
            }

            var items = paged.Items
                .Select(x =>
                {
                    var copy = _objectMapper.Map<ProductDto, ProductDto>(x);
                    copy.CostPrice = 0;
                    return copy;
                })
                .ToList();
            return new PagedResultDto<ProductDto>(paged.TotalCount, items);
        }

        /// <summary>
        /// bug-hunt 2026-10-19: chi tiết món cho kênh public — che giá vốn
        /// khỏi người không phải Admin (clone trước khi mask, giữ cache sạch).
        /// </summary>
        /// <param name="id">Id sản phẩm.</param>
        /// <returns>DTO chi tiết món đã che giá vốn (trừ Admin).</returns>
        public virtual async Task<ProductDto> GetPublicAsync(Guid id)
        {
            var dto = await GetAsync(id);
            if (CurrentUser?.IsInRole("Admin") == true)
            {
                return dto;
            }

            var copy = _objectMapper.Map<ProductDto, ProductDto>(dto);
            copy.CostPrice = 0;
            return copy;
        }

        /// <summary>
        /// Tạo món mới; mã món (code) không được trùng với món đã có.
        /// Cần quyền products.edit.
        /// </summary>
        /// <param name="input">Thông tin món cần tạo.</param>
        /// <returns>Món vừa tạo.</returns>
        public virtual async Task<ProductDto> CreateAsync(CreateUpdateProductDto input)
        {
            // Item 155: adding a product needs products.edit.
            await _permissions.RequirePermissionAsync(PermissionKeys.ProductsEdit);

            var code = ProductCode.Of(input.Code);

            if (await _products.ExistsWithCodeAsync(code))
            {
                throw new DuplicateProductCodeException(code.Value);
            }

            var product = Product.Create(code, input.Name, Money.Of(input.Price, input.Currency ?? "VND"), input.Description);
            ApplyEditableFields(product, input);

            product.CreatedAt = DateTime.UtcNow;
            await _products.InsertAsync(product);
            await _domainEvents.DispatchAsync(new EntityCreatedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActivityActions.ProductCreate,
                $"Thêm món {product.Name} ({product.Code.Value}).");

            return _objectMapper.Map<Product, ProductDto>(product);
        }

        /// <summary>
        /// Cập nhật thông tin món. Mã món không được đổi sau khi tạo vì sẽ gãy liên kết
        /// với đơn hàng/combo cũ đang tham chiếu mã đó. Chụp snapshot trước khi sửa để
        /// chỉ ghi log những trường thật sự thay đổi. Cần quyền products.edit.
        /// </summary>
        /// <param name="id">Id món.</param>
        /// <param name="input">Thông tin món mới.</param>
        /// <returns>Món sau khi cập nhật.</returns>
        public virtual async Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input)
        {
            // Item 155: editing a product needs products.edit.
            await _permissions.RequirePermissionAsync(PermissionKeys.ProductsEdit);

            var product = await FindOrThrowAsync(id);

            var code = ProductCode.Of(input.Code);
            if (!string.Equals(code.Value, product.Code.Value, StringComparison.Ordinal))
            {
                throw new Domain.Exceptions.DomainException("Product code cannot be changed.");
            }

            // Item 148: snapshot before mutation so only real changes are logged.
            var before = SnapshotFields(product);

            product.UpdateDetails(input.Name, input.Description);
            product.ChangePrice(Money.Of(input.Price, input.Currency ?? "VND"));
            ApplyEditableFields(product, input);

            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<ProductStatus>(input.Status, ignoreCase: true, out var status)
                && status != product.Status)
            {
                ApplyStatus(product, status);
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _products.UpdateAsync(product);
            await LogFieldChangesAsync(id, before, SnapshotFields(product));
            await _domainEvents.DispatchAsync(new EntityUpdatedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActivityActions.ProductUpdate,
                $"Sửa món {product.Name} ({product.Code.Value}).");

            return _objectMapper.Map<Product, ProductDto>(product);
        }

        /// <summary>
        /// Bật/tắt trạng thái còn bán của món (Active ↔ Inactive) mà không xóa món,
        /// để giữ nguyên lịch sử đơn hàng cũ. Cần quyền products.edit.
        /// </summary>
        /// <param name="id">Id món.</param>
        /// <returns>Món sau khi đổi trạng thái.</returns>
        public virtual async Task<ProductDto> ToggleAvailabilityAsync(Guid id)
        {
            // Item 155: toggling availability needs products.edit.
            await _permissions.RequirePermissionAsync(PermissionKeys.ProductsEdit);

            var product = await FindOrThrowAsync(id);
            var before = SnapshotFields(product);

            if (product.Status == ProductStatus.Active)
            {
                product.Deactivate();
            }
            else if (product.Status == ProductStatus.Inactive)
            {
                product.Activate();
            }
            else
            {
                throw new Domain.Exceptions.DomainException("A discontinued product cannot be toggled.");
            }

            await _products.UpdateAsync(product);
            // Item 148: the one-click sold-out toggle also lands in the history.
            await LogFieldChangesAsync(id, before, SnapshotFields(product));
            await _domainEvents.DispatchAsync(new EntityUpdatedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActivityActions.ProductUpdate,
                $"Bật/tắt món {product.Name} ({product.Code.Value}) → {product.Status}.");

            return _objectMapper.Map<Product, ProductDto>(product);
        }

        /// <summary>
        /// Item 146: saves the manual display order. Unknown ids are
        /// ignored; products not in the list keep their current order.
        /// </summary>
        public virtual async Task SaveDisplayOrderAsync(List<Guid> productIds)
        {
            // Item 155: reordering products needs products.edit.
            await _permissions.RequirePermissionAsync(PermissionKeys.ProductsEdit);

            if (productIds == null || productIds.Count == 0)
            {
                return;
            }

            var queryable = await _products.GetQueryableAsync();
            var ids = productIds.Distinct().ToList();
            var found = await _asyncExecuter.ToListAsync(queryable.Where(x => ids.Contains(x.Id)));
            var byId = found.ToDictionary(x => x.Id);

            for (var i = 0; i < ids.Count; i++)
            {
                if (byId.TryGetValue(ids[i], out var product))
                {
                    product.ChangeSortOrder(i);
                    await _products.UpdateAsync(product);
                }
            }

            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Item 165: trạng thái giới hạn suất/ngày của món đang bán cho shop
        /// public ("Còn N suất"). Dùng 1 query gom theo ngày hôm nay (giờ Việt
        /// Nam) trên các đơn chưa hủy; món không có DailyLimit bị bỏ qua.
        /// Kết quả cache 60 giây qua <see cref="GetDailyRemainingAsync"/>.
        /// </summary>
        public virtual async Task<List<ProductDailyRemainingDto>> GetDailyRemainingAsync()
        {
            if (_memoryCache.TryGetValue(DailyRemainingCacheKey, out List<ProductDailyRemainingDto> cached))
            {
                return cached;
            }
            var result = await QueryDailyRemainingAsync();
            _memoryCache.Set(DailyRemainingCacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(DailyRemainingCacheSeconds)
            });
            return result;
        }

        /// <summary>
        /// Tính "Còn N suất" hôm nay (bản chưa qua cache): lấy các món có
        /// giới hạn suất/ngày đang bán, gom số đã bán hôm nay (đơn chưa hủy,
        /// theo giờ Việt Nam) từ bảng đơn hàng; còn lại = giới hạn − đã bán
        /// (không về âm).
        /// </summary>
        private async Task<List<ProductDailyRemainingDto>> QueryDailyRemainingAsync()
        {
            var productQueryable = await _products.GetQueryableAsync();
            var limited = await _asyncExecuter.ToListAsync(
                productQueryable
                    .Where(x => x.DailyLimit != null && x.Status == ProductStatus.Active)
                    .Select(x => new { x.Id, Limit = x.DailyLimit.Value }));

            if (limited.Count == 0)
            {
                return new List<ProductDailyRemainingDto>();
            }

            var today = VietnamTime.Today();
            var fromUtc = VietnamTime.DayStartUtc(today);
            var toUtc = VietnamTime.DayEndExclusiveUtc(today);

            // [codequality-smell 2026-10-20] Dùng query dùng chung
            // OrderStatisticsQuery (trước đây copy-paste đoạn Where/GroupBy
            // này y hệt ở OrderAppService.LoadTodaySoldByProductAsync).
            var orderQueryable = await _orders.GetQueryableAsync();
            var soldMap = await OrderStatisticsQuery.SoldByProductAsync(
                orderQueryable, _asyncExecuter, fromUtc, toUtc);

            return limited
                .Select(x =>
                {
                    var sold = soldMap.TryGetValue(x.Id, out var soldQty) ? soldQty : 0;
                    return new ProductDailyRemainingDto
                    {
                        ProductId = x.Id,
                        DailyLimit = x.Limit,
                        SoldToday = sold,
                        RemainingToday = Math.Max(0, x.Limit - sold)
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Item 148: change history of a product, newest first.
        /// perf-hunt 2026-10-19: Take(200) server-side — log grows one row
        /// per changed field per edit, never materialize the whole table.
        /// </summary>
        public virtual async Task<List<ProductChangeLogDto>> GetChangeLogAsync(Guid productId)
        {
            var queryable = await _changeLogs.GetQueryableAsync();
            var logs = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.ProductId == productId)
                    .OrderByDescending(x => x.ChangedAt)
                    .Take(MaxChangeLogRows));

            return _objectMapper.Map<List<ProductChangeLog>, List<ProductChangeLogDto>>(logs);
        }

        /// <summary>
        /// Item 148: display snapshot of the loggable fields, label → value.
        /// </summary>
        private static Dictionary<string, string> SnapshotFields(Product product)
        {
            return new Dictionary<string, string>
            {
                ["Tên món"] = product.Name,
                ["Mô tả"] = Truncate(product.Description, SnapshotDescriptionMaxLength),
                ["Giá bán"] = FormatMoney(product.Price.Amount),
                // Item 256: log cả giá KM để tra "sao hôm nay đắt hơn".
                ["Giá KM"] = product.PromoPrice.HasValue
                    ? FormatMoney(product.PromoPrice.Value)
                    : "Không KM",
                ["Giá vốn"] = FormatMoney(product.CostPrice),
                ["Đơn vị"] = product.Unit,
                ["Trạng thái"] = StatusLabel(product.Status),
                ["Nhóm món"] = product.Category,
                ["Tags"] = product.Tags,
                ["Dị ứng"] = product.AllergenInfo,
                ["Món mới"] = product.IsNew ? "Có" : "Không",
                ["Signature"] = product.IsSignature ? "Có" : "Không",
                ["Bán từ"] = FormatDate(product.AvailableFrom),
                ["Bán đến"] = FormatDate(product.AvailableTo),
            };
        }

        /// <summary>
        /// Item 148: writes one log row per field whose display value
        /// actually changed between the two snapshots.
        /// </summary>
        private async Task LogFieldChangesAsync(
            Guid productId,
            Dictionary<string, string> before,
            Dictionary<string, string> after)
        {
            var changedBy = CurrentUser?.UserName ?? "system";
            var now = DateTime.UtcNow;

            foreach (var kvp in after)
            {
                before.TryGetValue(kvp.Key, out var oldValue);
                var newValue = kvp.Value;
                if (string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
                {
                    continue;
                }

                await _changeLogs.InsertAsync(new ProductChangeLog
                {
                    ProductId = productId,
                    ChangedBy = changedBy,
                    ChangedAt = now,
                    FieldName = kvp.Key,
                    OldValue = Truncate(oldValue, ProductChangeLog.ValueMaxLength),
                    NewValue = Truncate(newValue, ProductChangeLog.ValueMaxLength),
                });
            }
        }

        /// <summary>
        /// Cắt chuỗi về độ dài tối đa (dùng cho snapshot nhật ký thay đổi —
        /// mô tả món dài không cần lưu hết).
        /// </summary>
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            // Range operator [..maxLength] thay Substring(0, maxLength):
            // ngắn gọn, không sợ nhầm startIndex/length.
            return value[..maxLength];
        }

        /// <summary>
        /// Định dạng tiền VND cho nhật ký thay đổi (vd "50.000₫") —
        /// thống nhất cách hiển thị giá trước/sau khi sửa.
        /// </summary>
        private static string FormatMoney(decimal amount)
        {
            return amount.ToString("N0") + "₫";
        }

        /// <summary>
        /// Định dạng ngày dd/MM/yyyy cho nhật ký thay đổi — định dạng quen
        /// thuộc với người Việt (khác định dạng mặc định en-US của máy chủ).
        /// </summary>
        private static string FormatDate(DateTime? date)
        {
            return date?.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Nhãn tiếng Việt của trạng thái món dùng trong nhật ký thay đổi
        /// ("Đang bán"/"Hết hàng"/"Ngừng bán") — đọc lịch sử dễ hơn enum tiếng Anh.
        /// </summary>
        private static string StatusLabel(ProductStatus status)
        {
            return status switch
            {
                ProductStatus.Active => "Đang bán",
                ProductStatus.Inactive => "Hết hàng",
                ProductStatus.Discontinued => "Ngừng bán",
                _ => status.ToString(),
            };
        }

        /// <summary>
        /// Áp trạng thái mới lên entity món qua các hành vi domain
        /// (Activate/Deactivate/Discontinue) thay vì gán trực tiếp — giữ
        /// nguyên các quy tắc nội bộ của entity khi đổi trạng thái.
        /// </summary>
        private static void ApplyStatus(Product product, ProductStatus status)
        {
            switch (status)
            {
                case ProductStatus.Active:
                    product.Activate();
                    break;
                case ProductStatus.Inactive:
                    product.Deactivate();
                    break;
                case ProductStatus.Discontinued:
                    product.Discontinue();
                    break;
                default:
                    throw new Domain.Exceptions.DomainException($"Unsupported product status '{status}'.");
            }
        }

        /// <summary>
        /// Tách từ CreateAsync/UpdateAsync: cả tạo mới và cập nhật dùng chung
        /// một bộ field chỉnh sửa (giá vốn, đơn vị, khung giờ bán, giới hạn
        /// suất/ngày, ảnh, chi nhánh, thông tin catalog). Không gồm tên, mô
        /// tả, giá bán, mã — vì mỗi bên xử lý khác nhau.
        /// </summary>
        private static void ApplyEditableFields(Product product, CreateUpdateProductDto input)
        {
            product.ChangeCostPrice(input.CostPrice);
            // Item 256: giá KM (null/0 = xóa KM).
            product.ChangePromoPrice(input.PromoPrice);
            if (!string.IsNullOrWhiteSpace(input.Unit))
            {
                product.ChangeUnit(input.Unit);
            }
            // Items 164/165/166: sale time windows, daily portion limit, photo gallery.
            product.ChangeAvailableHoursJson(input.AvailableHoursJson);
            product.ChangeDailyLimit(input.DailyLimit);
            product.ChangeImageUrlsJson(input.ImageUrlsJson);
            product.ChangeBranch(input.BranchId);
            product.UpdateCatalogInfo(
                input.Tags, input.Category, input.AllergenInfo,
                input.IsNew, input.IsSignature,
                input.AvailableFrom, input.AvailableTo);
        }

        /// <summary>
        /// Xóa món. Kiểm tra tham chiếu (đơn hàng, combo...) TRƯỚC khi xóa để tránh lỗi DB;
        /// món đã có lịch sử bán nên dùng "Ngưng bán" (ToggleAvailabilityAsync) thay vì xóa cứng.
        /// Cần quyền products.delete.
        /// </summary>
        /// <param name="id">Id món cần xóa.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            // Item 155: deleting a product needs products.delete.
            await _permissions.RequirePermissionAsync(PermissionKeys.ProductsDelete);

            var product = await FindOrThrowAsync(id);

            // Kiểm tra references TRƯỚC khi xóa để tránh DbUpdateException.
            // OrderLine.ProductId chứa product id khi IsCombo=false.
            var orderLinesQuery = await _orderLines.GetQueryableAsync();
            var hasOrderHistory = await _asyncExecuter.AnyAsync(
                orderLinesQuery.Where(x => x.ProductId == id && !x.IsCombo));
            var sizeOptionsQuery = await _sizeOptions.GetQueryableAsync();
            var hasSizeOptions = await _asyncExecuter.AnyAsync(
                sizeOptionsQuery.Where(x => x.ProductId == id));

            if (hasOrderHistory)
            {
                // Món đã có trong đơn hàng thì không xóa cứng được,
                // chuyển sang ngưng bán để giữ lịch sử đơn hàng.
                product.Discontinue();
                await _products.UpdateAsync(product);
                await _activityLogger.LogAsync(StaffActivityActions.ProductDelete,
                    $"Món {product.Name} ({product.Code.Value}) có lịch sử đơn hàng nên chuyển sang Ngưng bán.");
            }
            else
            {
                // Xóa các size options liên quan trước (không có FK cascade).
                // DeleteManyAsync (RemoveRange) thay vì DeleteAsync từng cái:
                // cùng ngữ nghĩa trong 1 UoW, gọn 1 lần gọi repository.
                var options = await _sizeOptions.GetListAsync(x => x.ProductId == id);
                await _sizeOptions.DeleteManyAsync(options);
                await _products.DeleteAsync(product);
                await _activityLogger.LogAsync(StaffActivityActions.ProductDelete,
                    $"Xóa món {product.Name} ({product.Code.Value}).");
            }

            await _domainEvents.DispatchAsync(new EntityDeletedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Wave 6b (item 26): public per-product rating aggregates. One
        /// GROUP BY query over the review lines of non-hidden reviews;
        /// cached for 5 minutes like the public catalog.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        /// <summary>
        /// Wave 6b (item 26): tổng hợp đánh giá từng món từ review đơn hàng
        /// (chỉ review không bị ẩn) — điểm trung bình + số lượt đánh giá mỗi
        /// món. Kết quả cache 5 phút để trang shop không query lại mỗi lần tải.
        /// </summary>
        public virtual async Task<List<ProductRatingDto>> GetProductRatingsAsync()
        {
            return await _catalogCache.GetOrAddAsync(
                "product-ratings",
                QueryProductRatingsAsync);
        }

        /// <summary>
        /// Truy vấn DB tính rating từng món (bản chưa qua cache): gom các
        /// dòng review không bị ẩn theo ProductId, tính điểm trung bình và
        /// số lượt đánh giá ngay trên SQL.
        /// </summary>
        private async Task<List<ProductRatingDto>> QueryProductRatingsAsync()
        {
            var reviewQueryable = await _reviews.GetQueryableAsync();
            var query = reviewQueryable
                .Where(x => !x.IsHidden)
                .SelectMany(x => x.Lines)
                .GroupBy(x => x.ProductId)
                .Select(g => new ProductRatingDto
                {
                    ProductId = g.Key,
                    AverageRating = g.Average(x => x.Rating),
                    ReviewCount = g.Count()
                });
            return await _asyncExecuter.ToListAsync(query);
        }

        /// <summary>
        /// Tìm món theo Id; ném <see cref="ProductNotFoundException"/> khi
        /// không tồn tại — gom 1 chỗ để các method sửa/xóa/bật-tắt báo lỗi
        /// nhất quán (404 thân thiện) thay vì mỗi nơi 1 cách.
        /// </summary>
        private async Task<Product> FindOrThrowAsync(Guid id)
        {
            var product = await _products.FindAsync(id);
            if (product == null)
            {
                throw new ProductNotFoundException(id);
            }

            return product;
        }

        /// <summary>
        /// Loads active size options for the given product DTOs so the shop
        /// can render the size picker without extra round-trips.
        /// </summary>
        private async Task AttachSizesAsync(IEnumerable<ProductDto> dtos)
        {
            var list = dtos.ToList();
            if (list.Count == 0)
            {
                return;
            }

            var ids = list.Select(x => x.Id).ToList();
            var queryable = await _sizeOptions.GetQueryableAsync();
            var sizes = await _asyncExecuter.ToListAsync(
                queryable.Where(x => ids.Contains(x.ProductId) && x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Price));

            // ToLookup: tra cứu O(1) theo từng sản phẩm thay vì quét cả list
            // cho mỗi DTO (O(n*m)). Thứ tự trong group giữ nguyên thứ tự
            // query (SortOrder, Price); key vắng trả về rỗng giống .Where()
            // không khớp — output DTO không đổi.
            var sizesByProduct = sizes.ToLookup(x => x.ProductId);

            foreach (var dto in list)
            {
                dto.Sizes = sizesByProduct[dto.Id]
                    .Select(x => new ProductSizeOptionDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        Name = x.Name,
                        Price = x.Price,
                        SortOrder = x.SortOrder,
                        IsActive = x.IsActive
                    })
                    .ToList();
            }
        }
    }
}
