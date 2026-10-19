using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Products.Components
{
    /// <summary>
    /// Code-behind của form thêm/sửa sản phẩm: đồng bộ giờ bán và ảnh từ
    /// JSON của sản phẩm vào editor, xử lý upload/xóa ảnh trên server.
    /// </summary>
    public partial class AddEditProduct
    {
        [CascadingParameter]
        Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [Inject]
        public ProductService ProductService { get; set; }

        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public ILogger<AddEditProduct> Logger { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public PhuThuongStickyRice.Web.Components.Admin.Services.AdminCommerceService AdminCommerceService { get; set; }

        [Parameter]
        public ProductModel Product { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("interop.focusElementById", "productName");
            }
        }

        protected async Task HandleValidSubmit()
        {
            // Chặn bấm đúp nút Lưu: 2 request CreateProductAsync liên tiếp
            // sẽ tạo 2 sản phẩm trùng nhau (server sinh Id mới mỗi lần gọi).
            if (_saving)
            {
                return;
            }

            _saving = true;
            _saveError = null;
            try
            {
                // Items 164/166: serialize the hours + gallery editors into the
                // product model before saving.
                Product.AvailableHoursJson = SerializeHours();
                Product.ImageUrlsJson = SerializeImages();
                var isNew = Product.Id == Guid.Empty;
                await (isNew ? ProductService.CreateProductAsync(Product) : ProductService.UpdateProductAsync(Product.Id, Product));
                Toasts.Show(isNew ? "Đã thêm sản phẩm thành công!" : "Đã lưu sản phẩm thành công!");
                NavManager.NavigateTo("/products");
            }
            catch (Exception ex)
            {
                // Giữ người dùng ở lại form + hiện lỗi thay vì sập error boundary.
                Logger.LogError(ex, "Lưu sản phẩm thất bại.");
                _saveError = "Lưu sản phẩm thất bại. Vui lòng thử lại.";
            }
            finally
            {
                _saving = false;
            }
        }

        private bool _saving;

        private string _saveError;

        // ---- Items 164/166: sale time windows + photo gallery editors ----

        // Số khung giờ bán tối đa cho phép trên 1 sản phẩm.
        private const int MaxHourWindows = 3;

        private readonly List<HourWindow> _hours = new List<HourWindow>();

        private string _imageUrl;

        private Guid _syncedProductId = Guid.Empty;

        private bool _uploadingImages;

        private string _imageError;

        private sealed class HourWindow
        {
            public TimeOnly? From { get; set; } = new TimeOnly(6, 0);

            public TimeOnly? To { get; set; } = new TimeOnly(10, 0);
        }

        /// <summary>Shape of one item returned by interop.compressImagesFromInput.</summary>
        private sealed class CompressedImage
        {
            public string Base64 { get; set; } = "";

            public string FileName { get; set; } = "image.jpg";

            public string ContentType { get; set; } = "image/jpeg";
        }

        /// <summary>
        /// Called from the razor's OnParametersSetAsync: loads the hours and
        /// gallery editors from the product (once per product).
        /// </summary>
        internal void SyncFromProduct()
        {
            if (Product == null || Product.Id == _syncedProductId)
            {
                return;
            }

            _syncedProductId = Product.Id;
            _hours.Clear();
            _imageUrl = null;
            _imageError = null;

            LoadHourWindowsFromJson(Product.AvailableHoursJson);
            LoadImageUrlFromJson(Product.ImageUrlsJson);
        }

        /// <summary>
        /// Nạp tối đa 3 khung giờ bán từ JSON vào editor. JSON cũ hỏng thì
        /// bỏ qua — editor hiện danh sách trống để user nhập lại, không sập form.
        /// </summary>
        private void LoadHourWindowsFromJson(string hoursJson)
        {
            if (string.IsNullOrWhiteSpace(hoursJson))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(hoursJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (_hours.Count >= MaxHourWindows)
                        {
                            break;
                        }

                        var from = el.TryGetProperty("from", out var fromEl) ? fromEl.GetString() : null;
                        var to = el.TryGetProperty("to", out var toEl) ? toEl.GetString() : null;
                        if (TimeOnly.TryParse(from, out var fromT) && TimeOnly.TryParse(to, out var toT))
                        {
                            _hours.Add(new HourWindow { From = fromT, To = toT });
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Cố ý bỏ qua: dữ liệu giờ bán cũ có thể không phải JSON
                // hợp lệ — khi đó editor chỉ hiện danh sách giờ trống để
                // user nhập lại, không được làm sập form.
            }
        }

        /// <summary>
        /// Nạp ảnh đầu tiên từ JSON gallery vào ô ảnh. JSON cũ hỏng thì
        /// bỏ qua — ô ảnh hiện trống để user tải lại, không sập form.
        /// </summary>
        private void LoadImageUrlFromJson(string imageUrlsJson)
        {
            if (string.IsNullOrWhiteSpace(imageUrlsJson))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(imageUrlsJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String)
                        {
                            var url = el.GetString()?.Trim();
                            if (!string.IsNullOrEmpty(url))
                            {
                                _imageUrl = url;
                                break;
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Cố ý bỏ qua: JSON ảnh cũ có thể hỏng — khi đó ô ảnh chỉ
                // hiện trống để user tải lại, không được làm sập form.
            }
        }

        private string SerializeHours()
        {
            var valid = _hours
                .Where(w => w.From.HasValue && w.To.HasValue && w.From.Value < w.To.Value)
                .Select(w => new
                {
                    from = w.From.Value.ToString("HH:mm"),
                    to = w.To.Value.ToString("HH:mm")
                })
                .ToList();
            return valid.Count == 0 ? null : JsonSerializer.Serialize(valid);
        }

        private string SerializeImages() =>
            string.IsNullOrEmpty(_imageUrl) ? null : JsonSerializer.Serialize(new[] { _imageUrl });

        private void AddHour()
        {
            if (_hours.Count < MaxHourWindows)
            {
                _hours.Add(new HourWindow());
            }
        }

        private void RemoveHour(HourWindow w) => _hours.Remove(w);

        private async Task OnImagesSelected()
        {
            _imageError = null;

            List<CompressedImage> files;
            try
            {
                files = await JSRuntime.InvokeAsync<List<CompressedImage>>(
                        "interop.compressImagesFromInput", "product-images-input", 1600, 0.85)
                    ?? new List<CompressedImage>();
            }
            catch (Exception)
            {
                _imageError = "Không đọc được ảnh đã chọn.";
                return;
            }

            if (files.Count == 0)
            {
                return;
            }

            var file = files[0];
            if (string.IsNullOrEmpty(file.Base64))
            {
                _imageError = "Không đọc được ảnh đã chọn.";
                return;
            }

            // Xóa ảnh cũ trên server trước khi upload ảnh mới (best-effort: log rồi bỏ qua nếu lỗi)
            if (!string.IsNullOrEmpty(_imageUrl))
            {
                try { await ProductService.DeleteProductImageAsync(_imageUrl); }
                catch (Exception ex) { Logger.LogDebug(ex, "Không xóa được ảnh cũ {ImageUrl} trước khi upload — bỏ qua.", _imageUrl); }
            }

            _uploadingImages = true;
            try
            {
                using var stream = new MemoryStream(Convert.FromBase64String(file.Base64));
                var url = await ProductService.UploadProductImageAsync(
                    stream, file.FileName, file.ContentType);
                if (!string.IsNullOrEmpty(url))
                {
                    _imageUrl = url;
                }
            }
            catch (Exception ex)
            {
                _imageError = ex.Message;
            }
            finally
            {
                _uploadingImages = false;
            }
        }

        private async Task RemoveImage()
        {
            if (!string.IsNullOrEmpty(_imageUrl))
            {
                // Xóa ảnh trên server là best-effort: log rồi bỏ qua nếu lỗi.
                try { await ProductService.DeleteProductImageAsync(_imageUrl); }
                catch (Exception ex) { Logger.LogDebug(ex, "Không xóa được ảnh {ImageUrl} trên server — bỏ qua.", _imageUrl); }
                _imageUrl = null;
            }
        }
    }
}
