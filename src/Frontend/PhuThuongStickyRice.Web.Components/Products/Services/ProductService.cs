using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Products.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Products.Services
{
    public class ProductService : HttpService
    {
        public ProductService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<List<ProductModel>> GetProductsAsync()
        {
            // The API returns a paged result; unwrap the items list.
            var paged = await GetAsync<PagedResultModel<ProductModel>>("api/products");
            return paged?.Items ?? new List<ProductModel>();
        }

        public async Task<ProductModel> GetProductByIdAsync(Guid id)
        {
            var product = await GetAsync<ProductModel>($"api/products/{id}");
            return product;
        }

        public async Task<ProductModel> CreateProductAsync(ProductModel product)
        {
            var createdProduct = await PostAsync<ProductModel>("api/products", product);
            return createdProduct;
        }

        public async Task<ProductModel> UpdateProductAsync(Guid id, ProductModel product)
        {
            var updatedProduct = await PutAsync<ProductModel>($"api/products/{id}", product);
            return updatedProduct;
        }

        public async Task DeleteProductAsync(Guid id)
        {
            await DeleteAsync($"api/products/{id}");
        }

        /// <summary>One-click sold-out toggle (Active &lt;-&gt; Inactive).</summary>
        public async Task<ProductModel> ToggleAvailabilityAsync(Guid id)
        {
            return await PostAsync<ProductModel>($"api/products/{id}/toggle-availability", new { });
        }

        /// <summary>
        /// Item 146: saves the manual display order (drag &amp; drop on
        /// /products). Ids are in display order.
        /// </summary>
        public async Task ReorderAsync(List<Guid> ids)
        {
            await PostAsync<object>("api/products/reorder", new { ids });
        }

        /// <summary>
        /// Item 148: change history of a product ("ai sửa gì, khi nào"),
        /// newest first.
        /// </summary>
        public async Task<List<ProductChangeLogModel>> GetChangeLogAsync(Guid id)
        {
            var logs = await GetAsync<List<ProductChangeLogModel>>($"api/products/{id}/change-log");
            return logs ?? new List<ProductChangeLogModel>();
        }

        /// <summary>Item 130: whether FB auto-post is configured (Page ID + token).</summary>
        public async Task<FacebookStatusModel> GetFacebookStatusAsync()
        {
            return await GetAsync<FacebookStatusModel>("api/admin/facebook/status");
        }

        /// <summary>Item 130: publishes a product announcement to the FB Page feed now.</summary>
        public async Task<FacebookPublishResultModel> PublishProductToFacebookAsync(Guid id)
        {
            return await PostAsync<FacebookPublishResultModel>($"api/admin/facebook/publish-product/{id}", new { });
        }

        /// <summary>Item 130: recent FB auto-post history, newest first.</summary>
        public async Task<List<FacebookPostLogModel>> GetFacebookPostHistoryAsync(int maxCount = 50)
        {
            return await GetAsync<List<FacebookPostLogModel>>($"api/admin/facebook/posts?maxCount={maxCount}");
        }

        public string GetExportPdfUrl()
        {
            return $"{_httpClient.BaseAddress.AbsoluteUri.Trim('/')}/api/products/ExportAsPdf";
        }

        public string GetExportCsvUrl()
        {
            return $"{_httpClient.BaseAddress.AbsoluteUri.Trim('/')}/api/products/ExportAsCsv";
        }
    }
}
