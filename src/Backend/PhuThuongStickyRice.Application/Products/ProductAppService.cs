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
    }

    public class ProductAppService : ApplicationService, IProductAppService
    {
        private readonly IProductRepository _products;
        private readonly IDomainEvents _domainEvents;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;

        public ProductAppService(
            IProductRepository products,
            IDomainEvents domainEvents,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper)
        {
            _products = products;
            _domainEvents = domainEvents;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
        }

        public async Task<PagedResultDto<ProductDto>> GetListAsync(GetProductsInput input)
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
            }

            var totalCount = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

            return new PagedResultDto<ProductDto>(
                totalCount,
                _objectMapper.Map<List<Product>, List<ProductDto>>(items));
        }

        public async Task<ProductDto> GetAsync(Guid id)
        {
            var product = await FindOrThrowAsync(id);
            return _objectMapper.Map<Product, ProductDto>(product);
        }

        public async Task<ProductDto> CreateAsync(CreateUpdateProductDto input)
        {
            var code = ProductCode.Of(input.Code);

            if (await _products.ExistsWithCodeAsync(code))
            {
                throw new DuplicateProductCodeException(code.Value);
            }

            var product = Product.Create(code, input.Name, Money.Of(input.Price, input.Currency ?? "VND"), input.Description);

            await _products.InsertAsync(product);
            await _domainEvents.DispatchAsync(new EntityCreatedEvent<Product>(product, DateTime.UtcNow));

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

            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<ProductStatus>(input.Status, ignoreCase: true, out var status)
                && status != product.Status)
            {
                ApplyStatus(product, status);
            }

            await _products.UpdateAsync(product);
            await _domainEvents.DispatchAsync(new EntityUpdatedEvent<Product>(product, DateTime.UtcNow));

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
    }
}
