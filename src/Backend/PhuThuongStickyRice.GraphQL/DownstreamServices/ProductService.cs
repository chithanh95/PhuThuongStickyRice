using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using PhuThuongStickyRice.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.GraphQL.DownstreamServices
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ProductModel>> GetProducts()
        {
            SetBearerToken();

            var response = await _httpClient.GetAsync("api/products", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var paged = await response.Content.ReadAs<PagedModel<ProductModel>>();
            return paged?.Items ?? new List<ProductModel>();
        }

        private void SetBearerToken()
        {

        }

        public async Task<ProductModel> GetProductById(Guid id)
        {
            SetBearerToken();

            var response = await _httpClient.GetAsync($"api/products/{id}", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var product = await response.Content.ReadAs<ProductModel>();
            return product;
        }

        public async Task<ProductModel> CreateProduct(ProductModel product)
        {
            SetBearerToken();

            var request = new HttpRequestMessage(HttpMethod.Post, "api/products");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = product.AsJsonContent();

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var createdProduct = await response.Content.ReadAs<ProductModel>();
            return createdProduct;
        }

        public async Task<ProductModel> UpdateProduct(Guid id, ProductModel product)
        {
            SetBearerToken();

            var request = new HttpRequestMessage(HttpMethod.Put, $"api/products/{id}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = product.AsJsonContent();

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var updatedProduct = await response.Content.ReadAs<ProductModel>();
            return updatedProduct;
        }

        public async Task DeleteProduct(Guid id)
        {
            SetBearerToken();

            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/products/{id}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        }
    }
}
