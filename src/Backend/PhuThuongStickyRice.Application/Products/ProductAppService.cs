using PhuThuongStickyRice.Application.Products.DTOs;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using Volo.Abp.ObjectMapping;

namespace PhuThuongStickyRice.Application.Products
{
    public interface IProductAppService : IApplicationService
    {
        Task<PagedResultDto<ProductDto>> GetListAsync(GetProductsInput input);

        Task<ProductDto> GetAsync(Guid id);

        Task<ProductDto> CreateAsync(CreateUpdateProductDto input);

        Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input);

        Task DeleteAsync(Guid id);

        /// <summary>
        /// One-click sold-out toggle: Active -> Inactive -> Active.
        /// Discontinued products cannot be toggled.
        /// </summary>
        Task<ProductDto> ToggleAvailabilityAsync(Guid id);

        /// <summary>
        /// Wave 6b (item 26): public per-product rating aggregates from
        /// non-hidden order reviews. Products without reviews are omitted.
        /// </summary>
        Task<List<ProductRatingDto>> GetProductRatingsAsync();
    }

    public class ProductAppService : ApplicationService, IProductAppService
    {
        private readonly IProductRepository _products;
        private readonly IDomainEvents _domainEvents;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;
        private readonly Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> _sizeOptions;
        private readonly IProductCatalogCache _catalogCache;
        private readonly Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> _reviews;

        public ProductAppService(
            IProductRepository products,
            IDomainEvents domainEvents,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper,
            Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> sizeOptions,
            IProductCatalogCache catalogCache,
            Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> reviews)
        {
            _products = products;
            _domainEvents = domainEvents;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
            _sizeOptions = sizeOptions;
            _catalogCache = catalogCache;
            _reviews = reviews;
        }

        public async Task<PagedResultDto<ProductDto>> GetListAsync(GetProductsInput input)
        {
            // Item 101: the public shop path (status=Active) is served from a
            // 5-minute in-memory cache. The admin list (no status filter)
            // always hits the database.
            var isPublicPath = string.Equals(input.Status, "Active", StringComparison.OrdinalIgnoreCase);
            if (!isPublicPath)
            {
                return await QueryListAsync(input);
            }

            var cacheKey = "products:active:"
                + (input.SearchTerm ?? string.Empty).Trim().ToLowerInvariant() + ":"
                + (input.BranchId?.ToString() ?? "all") + ":"
                + input.SkipCount + ":" + input.MaxResultCount;
            return await _catalogCache.GetOrAddAsync(cacheKey, () => QueryListAsync(input));
        }

        private async Task<PagedResultDto<ProductDto>> QueryListAsync(GetProductsInput input)
        {
            var queryable = await _products.GetQueryableAsync();
            System.Linq.IQueryable<Product> query = queryable.OrderBy(x => x.Name);

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x => x.Name.Contains(term) || x.Code.Value.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<ProductStatus>(input.Status, ignoreCase: true, out var status))
            {
                query = query.Where(x => x.Status == status);

                // The public shop asks for status=Active: hide seasonal
                // products that are outside their availability window.
                // The admin list (no status filter) still sees them all.
                if (status == ProductStatus.Active)
                {
                    var today = DateTime.UtcNow.Date;
                    query = query.Where(x =>
                        (x.AvailableFrom == null || x.AvailableFrom.Value.Date <= today)
                        && (x.AvailableTo == null || x.AvailableTo.Value.Date >= today));
                }
            }

            // Item 109: branch filter — products of the branch plus
            // branch-less (global) products.
            if (input.BranchId.HasValue)
            {
                var branchId = input.BranchId.Value;
                query = query.Where(x => x.BranchId == null || x.BranchId == branchId);
            }

            var totalCount = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

            var dtos = _objectMapper.Map<List<Product>, List<ProductDto>>(items);
            await AttachSizesAsync(dtos);

            return new PagedResultDto<ProductDto>(totalCount, dtos);
        }

        public async Task<ProductDto> GetAsync(Guid id)
        {
            var product = await FindOrThrowAsync(id);
            var dto = _objectMapper.Map<Product, ProductDto>(product);
            await AttachSizesAsync(new[] { dto });
            return dto;
        }

        public async Task<ProductDto> CreateAsync(CreateUpdateProductDto input)
        {
            var code = ProductCode.Of(input.Code);

            if (await _products.ExistsWithCodeAsync(code))
            {
                throw new DuplicateProductCodeException(code.Value);
            }

            var product = Product.Create(code, input.Name, Money.Of(input.Price, input.Currency ?? "VND"), input.Description);
            product.ChangeCostPrice(input.CostPrice);
            product.ChangeBranch(input.BranchId);
            product.UpdateCatalogInfo(
                input.Tags, input.Category, input.AllergenInfo,
                input.IsNew, input.IsSignature,
                input.AvailableFrom, input.AvailableTo);

            await _products.InsertAsync(product);
            await _domainEvents.DispatchAsync(new EntityCreatedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();

            return _objectMapper.Map<Product, ProductDto>(product);
        }

        public async Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input)
        {
            var product = await FindOrThrowAsync(id);

            var code = ProductCode.Of(input.Code);
            if (!string.Equals(code.Value, product.Code.Value, StringComparison.Ordinal))
            {
                throw new Domain.Exceptions.DomainException("Product code cannot be changed.");
            }

            product.UpdateDetails(input.Name, input.Description);
            product.ChangePrice(Money.Of(input.Price, input.Currency ?? "VND"));
            product.ChangeCostPrice(input.CostPrice);
            product.UpdateCatalogInfo(
                input.Tags, input.Category, input.AllergenInfo,
                input.IsNew, input.IsSignature,
                input.AvailableFrom, input.AvailableTo);

            product.ChangeBranch(input.BranchId);

            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<ProductStatus>(input.Status, ignoreCase: true, out var status)
                && status != product.Status)
            {
                ApplyStatus(product, status);
            }

            await _products.UpdateAsync(product);
            await _domainEvents.DispatchAsync(new EntityUpdatedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();

            return _objectMapper.Map<Product, ProductDto>(product);
        }

        public async Task<ProductDto> ToggleAvailabilityAsync(Guid id)
        {
            var product = await FindOrThrowAsync(id);

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
            await _domainEvents.DispatchAsync(new EntityUpdatedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();

            return _objectMapper.Map<Product, ProductDto>(product);
        }

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

        public async Task DeleteAsync(Guid id)
        {
            var product = await FindOrThrowAsync(id);

            await _products.DeleteAsync(product);
            await _domainEvents.DispatchAsync(new EntityDeletedEvent<Product>(product, DateTime.UtcNow));
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Wave 6b (item 26): public per-product rating aggregates. One
        /// GROUP BY query over the review lines of non-hidden reviews;
        /// cached for 5 minutes like the public catalog.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<List<ProductRatingDto>> GetProductRatingsAsync()
        {
            return await _catalogCache.GetOrAddAsync(
                "product-ratings",
                QueryProductRatingsAsync);
        }

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

            foreach (var dto in list)
            {
                dto.Sizes = sizes
                    .Where(x => x.ProductId == dto.Id)
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
