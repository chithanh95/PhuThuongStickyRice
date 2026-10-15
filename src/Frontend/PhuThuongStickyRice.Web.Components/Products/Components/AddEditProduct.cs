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
            // Items 164/166: serialize the hours + gallery editors into the
            // product model before saving.
            Product.AvailableHoursJson = SerializeHours();
            Product.ImageUrlsJson = SerializeImages();
            await (Product.Id == Guid.Empty ? ProductService.CreateProductAsync(Product) : ProductService.UpdateProductAsync(Product.Id, Product));
            NavManager.NavigateTo("/products");
        }

        // ---- Items 164/166: sale time windows + photo gallery editors ----

        private readonly List<HourWindow> _hours = new List<HourWindow>();

        private readonly List<string> _images = new List<string>();

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
            public byte[] Bytes { get; set; } = Array.Empty<byte>();

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
            _images.Clear();
            _imageError = null;

            if (!string.IsNullOrWhiteSpace(Product.AvailableHoursJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(Product.AvailableHoursJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            if (_hours.Count >= 3)
                            {
                                break;
                            }

                            var from = el.TryGetProperty("from", out var f) ? f.GetString() : null;
                            var to = el.TryGetProperty("to", out var t) ? t.GetString() : null;
                            if (TimeOnly.TryParse(from, out var fromT) && TimeOnly.TryParse(to, out var toT))
                            {
                                _hours.Add(new HourWindow { From = fromT, To = toT });
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(Product.ImageUrlsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(Product.ImageUrlsJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            if (_images.Count >= 10)
                            {
                                break;
                            }

                            if (el.ValueKind == JsonValueKind.String)
                            {
                                var url = el.GetString()?.Trim();
                                if (!string.IsNullOrEmpty(url))
                                {
                                    _images.Add(url);
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                }
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
            _images.Count == 0 ? null : JsonSerializer.Serialize(_images);

        private void AddHour()
        {
            if (_hours.Count < 3)
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

            if (_images.Count + files.Count > 10)
            {
                _imageError = "Tối đa 10 ảnh cho mỗi món.";
                return;
            }

            _uploadingImages = true;
            try
            {
                foreach (var f in files)
                {
                    using var stream = new MemoryStream(f.Bytes);
                    var url = await ProductService.UploadProductImageAsync(
                        stream, f.FileName, f.ContentType);
                    if (!string.IsNullOrEmpty(url))
                    {
                        _images.Add(url);
                    }
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

        private void RemoveImage(int index)
        {
            if (index >= 0 && index < _images.Count)
            {
                _images.RemoveAt(index);
            }
        }

        private void SetPrimaryImage(int index)
        {
            if (index > 0 && index < _images.Count)
            {
                var url = _images[index];
                _images.RemoveAt(index);
                _images.Insert(0, url);
            }
        }
    }
}
