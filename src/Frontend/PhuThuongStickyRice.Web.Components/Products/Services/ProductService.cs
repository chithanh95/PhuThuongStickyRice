using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Products.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Products.Services
{
    /// <summary>
    /// Service gọi API sản phẩm cho màn hình quản trị (/products).
    /// Tất cả request đều tự gắn Bearer token qua <see cref="ApiClientBase"/>.
    /// </summary>
    public class ProductService : ApiClientBase
    {
        public ProductService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Lấy danh sách sản phẩm (tự bóc danh sách items từ kết quả phân trang của API).</summary>
        public async Task<List<ProductModel>> GetProductsAsync()
        {
            // The API returns a paged result; unwrap the items list.
            var paged = await GetAsync<PagedResultModel<ProductModel>>("api/products");
            return paged?.Items ?? new List<ProductModel>();
        }

        /// <summary>Lấy chi tiết một sản phẩm theo Id.</summary>
        public async Task<ProductModel> GetProductByIdAsync(Guid id)
        {
            var product = await GetAsync<ProductModel>($"api/products/{id}");
            return product;
        }

        /// <summary>Tạo sản phẩm mới.</summary>
        public async Task<ProductModel> CreateProductAsync(ProductModel product)
        {
            var createdProduct = await PostAsync<ProductModel>("api/products", product);
            return createdProduct;
        }

        /// <summary>Cập nhật sản phẩm theo Id.</summary>
        public async Task<ProductModel> UpdateProductAsync(Guid id, ProductModel product)
        {
            var updatedProduct = await PutAsync<ProductModel>($"api/products/{id}", product);
            return updatedProduct;
        }

        /// <summary>
        /// Xóa sản phẩm. API tự chặn (hoặc chuyển thành "Ngưng bán") nếu
        /// sản phẩm đã có lịch sử đơn hàng — xem ProductAppService.
        /// </summary>
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
        public async Task SaveDisplayOrderAsync(List<Guid> ids)
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

        /// <summary>
        /// Item 166: uploads one (already compressed) product photo.
        /// Returns the stored image URL (e.g. "/uploads/products/xxx.jpg").
        /// </summary>
        public async Task<string> UploadProductImageAsync(
            System.IO.Stream content, string fileName, string contentType)
        {
            await SetBearerToken();

            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            // HttpResponseMessage giữ kết nối/stream phía dưới — dispose ngay
            // sau khi đọc xong body để trả kết nối về pool (CA2000).
            using var response = await _httpClient.PostAsync("api/products/images/upload", form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var message = "Tải ảnh lên thất bại.";
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        message = messageElement.GetString() ?? message;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Best-effort: error body isn't JSON — keep the default message.
                }

                throw new System.InvalidOperationException(message);
            }

            try
            {
                using var okDoc = System.Text.Json.JsonDocument.Parse(body);
                if (okDoc.RootElement.TryGetProperty("url", out var urlElement))
                {
                    return urlElement.GetString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Best-effort: success body has no url — fall through to the error below.
            }

            throw new System.InvalidOperationException("Tải ảnh lên thất bại.");
        }

        /// <summary>Xóa ảnh sản phẩm trên server theo URL đã lưu.</summary>
        public async Task DeleteProductImageAsync(string url)
        {
            await SetBearerToken();
            // URL ảnh có thể chứa ký tự đặc biệt — phải encode trước khi đưa vào query string.
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.DeleteAsync(
                $"api/products/images?url={Uri.EscapeDataString(url)}");
            response.EnsureSuccessStatusCode();
        }

        /// <summary>URL tải danh sách sản phẩm dạng PDF (mở trang export của API trong tab mới).</summary>
        public string GetExportPdfUrl()
        {
            return $"{_httpClient.BaseAddress.AbsoluteUri.Trim('/')}/api/products/ExportAsPdf";
        }

        /// <summary>URL tải danh sách sản phẩm dạng CSV.</summary>
        public string GetExportCsvUrl()
        {
            return $"{_httpClient.BaseAddress.AbsoluteUri.Trim('/')}/api/products/ExportAsCsv";
        }
    }
}
