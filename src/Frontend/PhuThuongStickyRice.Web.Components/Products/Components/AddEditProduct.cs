using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public NavigationManager Navigation { get; set; }

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
                // Ảnh cũ bị thay thế/loại bỏ trong lúc sửa chỉ xóa thật khi
                // đã lưu thành công — tránh gãy ảnh nếu upload lỗi/bỏ lưu.
                await DeleteObsoleteImagesAsync();
                Toasts.Show(isNew ? "Đã thêm sản phẩm thành công!" : "Đã lưu sản phẩm thành công!");
                Navigation.NavigateTo("/products");
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

        // Chiều rộng tối đa (px) khi nén ảnh sản phẩm bằng JS interop trước upload —
        // ảnh chụp điện thoại thường 3000–4000px, nén xuống 1600px vẫn đủ nét
        // hiển thị mà giảm hẳn dung lượng upload/lưu trữ.
        private const int CompressedImageMaxWidth = 1600;

        // Chất lượng JPEG khi nén ảnh (0–1): 0.85 là điểm cân bằng nét/dung lượng.
        private const double CompressedImageQuality = 0.85;

        // Thông báo khi file ảnh chọn từ input không đọc được (file hỏng/không phải ảnh).
        private const string UnreadableImageError = "Không đọc được ảnh đã chọn.";

        // [codequality-smell 2026-10-20] Magic string: id của input file ảnh.
        // Phải khớp nhau ở 3 nơi — JS interop (compressImagesFromInput), thuộc
        // tính for của label và id của input trong .razor — nên gom 1 hằng số
        // để đổi 1 chỗ không lệch.
        private const string ImageInputElementId = "product-images-input";

        private readonly List<HourWindow> _hours = new List<HourWindow>();

        private string _imageUrl;

        /// <summary>
        /// URL ảnh đã bị thay thế/xóa trong lúc chỉnh sửa nhưng chưa lưu —
        /// chỉ xóa thật trên server SAU KHI lưu sản phẩm thành công, để upload
        /// lỗi hoặc user bỏ không lưu không làm gãy ảnh đang dùng.
        /// </summary>
        private readonly List<string> _obsoleteImageUrls = new List<string>();

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
            _obsoleteImageUrls.Clear();

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
                    foreach (var hourElement in doc.RootElement.EnumerateArray())
                    {
                        if (_hours.Count >= MaxHourWindows)
                        {
                            break;
                        }

                        var from = hourElement.TryGetProperty("from", out var fromEl) ? fromEl.GetString() : null;
                        var to = hourElement.TryGetProperty("to", out var toEl) ? toEl.GetString() : null;
                        if (TimeOnly.TryParse(from, out var fromTime) && TimeOnly.TryParse(to, out var toTime))
                        {
                            _hours.Add(new HourWindow { From = fromTime, To = toTime });
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
                    foreach (var imageElement in doc.RootElement.EnumerateArray())
                    {
                        if (imageElement.ValueKind == JsonValueKind.String)
                        {
                            var url = imageElement.GetString()?.Trim();
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
                    // CA1305: serialize giờ để lưu DB/JSON phải dùng
                    // InvariantCulture — ký tự ":" trong format "HH:mm" là time
                    // separator phụ thuộc locale (máy user có thể render khác),
                    // InvariantCulture cho ra "HH:mm" ổn định mọi môi trường.
                    from = w.From.Value.ToString("HH:mm", CultureInfo.InvariantCulture),
                    to = w.To.Value.ToString("HH:mm", CultureInfo.InvariantCulture)
                })
                .ToList();
            return valid.Count == 0 ? null : JsonSerializer.Serialize(valid);
        }

        private string SerializeImages() =>
            string.IsNullOrEmpty(_imageUrl) ? null : JsonSerializer.Serialize(new[] { _imageUrl });

        private async Task OnImagesSelected()
        {
            _imageError = null;

            List<CompressedImage> files;
            try
            {
                files = await JSRuntime.InvokeAsync<List<CompressedImage>>(
                        "interop.compressImagesFromInput", ImageInputElementId, CompressedImageMaxWidth, CompressedImageQuality)
                    ?? new List<CompressedImage>();
            }
            catch (Exception)
            {
                _imageError = UnreadableImageError;
                return;
            }

            if (files.Count == 0)
            {
                return;
            }

            var file = files[0];
            if (string.IsNullOrEmpty(file.Base64))
            {
                _imageError = UnreadableImageError;
                return;
            }

            await TryUploadImageAsync(file);
        }

        /// <summary>
        /// Upload ảnh đã chọn lên server. Ảnh cũ KHÔNG xóa ngay tại đây:
        /// nếu upload mới lỗi hoặc user không bấm Lưu, xóa sớm sẽ làm gãy
        /// ảnh sản phẩm đang dùng — URL cũ được đưa vào
        /// <c>_obsoleteImageUrls</c> và chỉ xóa thật sau khi lưu sản phẩm
        /// thành công. Lỗi upload được ghi vào <c>_imageError</c>.
        /// </summary>
        /// <param name="file">Ảnh đã nén đọc từ input file.</param>
        /// <returns>URL ảnh mới, hoặc null/rỗng nếu upload thất bại.</returns>
        private async Task<string> TryUploadImageAsync(CompressedImage file)
        {
            _uploadingImages = true;
            try
            {
                using var stream = new MemoryStream(Convert.FromBase64String(file.Base64));
                var url = await ProductService.UploadProductImageAsync(
                    stream, file.FileName, file.ContentType);
                if (!string.IsNullOrEmpty(url))
                {
                    if (!string.IsNullOrEmpty(_imageUrl)
                        && !string.Equals(_imageUrl, url, StringComparison.OrdinalIgnoreCase)
                        && !_obsoleteImageUrls.Contains(_imageUrl))
                    {
                        _obsoleteImageUrls.Add(_imageUrl);
                    }

                    _imageUrl = url;
                }
                return url;
            }
            catch (Exception ex)
            {
                _imageError = ex.Message;
                return null;
            }
            finally
            {
                _uploadingImages = false;
            }
        }

        private void RemoveImage()
        {
            if (!string.IsNullOrEmpty(_imageUrl))
            {
                // Không xóa ngay trên server: user có thể đóng trang mà không
                // bấm Lưu — xóa sớm sẽ làm gãy ảnh dù DB vẫn trỏ URL cũ.
                // URL được đưa vào _obsoleteImageUrls, xóa thật sau khi lưu.
                if (!_obsoleteImageUrls.Contains(_imageUrl))
                {
                    _obsoleteImageUrls.Add(_imageUrl);
                }

                _imageUrl = null;
            }
        }

        /// <summary>
        /// Xóa các ảnh đã bị thay thế/loại bỏ trong lúc chỉnh sửa — chỉ gọi
        /// sau khi lưu sản phẩm thành công. Best-effort: log rồi bỏ qua nếu lỗi.
        /// </summary>
        private async Task DeleteObsoleteImagesAsync()
        {
            foreach (var url in _obsoleteImageUrls)
            {
                if (string.Equals(url, _imageUrl, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    await ProductService.DeleteProductImageAsync(url);
                }
                catch (Exception ex)
                {
                    // CA1873: format exception (stack trace) tốn kém — chỉ
                    // đánh giá khi log Debug đang bật.
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(ex, "Không xóa được ảnh cũ {ImageUrl} trên server — bỏ qua.", url);
                    }
                }
            }

            _obsoleteImageUrls.Clear();
        }
    }
}
