using PhuThuongStickyRice.Application.Products;
using PhuThuongStickyRice.Application.Products.DTOs;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.CrossCuttingConcerns.Csv;
using PhuThuongStickyRice.CrossCuttingConcerns.HtmlGenerator;
using PhuThuongStickyRice.CrossCuttingConcerns.PdfConverter;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.Authorization;
using PhuThuongStickyRice.WebAPI.Models.Products;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    // Item 107: versioned prefix — both routes stay live.
    [Route("api/v1/[controller]")]
    [ApiController]
    /// <summary>
    /// API sản phẩm (catalog xôi): danh sách/chi tiết public cho shop
    /// (GET mở, có giới hạn pageSize), còn tạo/sửa/xóa/upload ảnh chỉ role
    /// Admin và theo policy chi tiết. Định dạng ảnh: jpg/jpeg/png/webp/gif,
    /// tối đa 5 MB, kiểm tra magic bytes khi upload.
    /// </summary>
    public partial class ProductsController : ControllerBase
    {
        private readonly IProductAppService _productAppService;
        private readonly ILogger _logger;
        private readonly IHtmlGenerator _htmlGenerator;
        private readonly IPdfConverter _pdfConverter;
        private readonly ICsvWriter<ProductModel> _productCsvWriter;
        private readonly ICsvReader<ProductModel> _productCsvReader;
        private readonly PhuThuongStickyRice.Application.Orders.IDashboardAppService _dashboardAppService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _environment;

        private const long MaxProductImageBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        public ProductsController(IProductAppService productAppService,
            ILogger<ProductsController> logger,
            IHtmlGenerator htmlGenerator,
            IPdfConverter pdfConverter,
            ICsvWriter<ProductModel> productCsvWriter,
            ICsvReader<ProductModel> productCsvReader,
            PhuThuongStickyRice.Application.Orders.IDashboardAppService dashboardAppService,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
        {
            _productAppService = productAppService;
            _logger = logger;
            _htmlGenerator = htmlGenerator;
            _pdfConverter = pdfConverter;
            _productCsvWriter = productCsvWriter;
            _productCsvReader = productCsvReader;
            _dashboardAppService = dashboardAppService;
            _environment = environment;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm phân trang (public — shop dùng). pageSize bị
        /// kẹp trong 1..100 để request ngoài internet không buộc đọc cả bảng.
        /// </summary>
        /// <param name="page">Trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số món/trang, tối đa 100.</param>
        /// <param name="searchTerm">Tìm theo tên/mã.</param>
        /// <param name="status">Lọc trạng thái (Active/Inactive).</param>
        /// <param name="branch">Lọc theo chi nhánh.</param>
        /// <param name="includeInactive">Bao gồm món đã ngưng bán.</param>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string searchTerm = null, [FromQuery] string status = null,
            [FromQuery] Guid? branch = null,
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            LogGettingProducts(_logger);
            // Perf: pageSize comes from the public internet — clamp it so a huge
            // value can't force a full-table materialization + poison the
            // 5-minute public cache with a giant list. Internal callers
            // (export PDF/CSV, Messenger) bypass this via the service directly.
            var effectivePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
            var paged = await _productAppService.GetPublicListAsync(new GetProductsInput
            {
                MaxResultCount = effectivePageSize,
                SkipCount = (page < 1 ? 0 : page - 1) * effectivePageSize,
                SearchTerm = searchTerm,
                Status = status,
                BranchId = branch,
                IncludeInactive = includeInactive
            });
            return Ok(paged);
        }

        /// <summary>Lấy chi tiết 1 sản phẩm theo id (public). Không tìm thấy → 404.</summary>
        /// <param name="id">Id sản phẩm.</param>
        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                // bug-hunt 2026-10-19: endpoint public — giá vốn chỉ hiện cho Admin.
                var dto = await _productAppService.GetPublicAsync(id);
                return Ok(dto);
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Tạo sản phẩm mới (Admin, quyền AddProductPolicy). Trạng thái do
        /// service đặt mặc định (khác Put cho phép sửa Status). Trả 201 + DTO
        /// đã tạo; lỗi nghiệp vụ/trùng mã → 400.
        /// </summary>
        /// <param name="model">Thông tin sản phẩm từ form admin.</param>
        [Authorize(AuthorizationPolicyNames.AddProductPolicy)]
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDto>> Post([FromBody] ProductModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _productAppService.CreateAsync(ToCreateUpdateDto(model));
                return Created($"/api/products/{created.Id}", created);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DuplicateProductCodeException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật sản phẩm (Admin, quyền UpdateProductPolicy). Cho phép đổi
        /// Status (khác Post). Không tìm thấy → 404, lỗi nghiệp vụ → 400.
        /// </summary>
        /// <param name="id">Id sản phẩm cần sửa.</param>
        /// <param name="model">Thông tin sản phẩm mới từ form admin.</param>
        [Authorize(AuthorizationPolicyNames.UpdateProductPolicy)]
        [HttpPut("{id}")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDto>> Put(Guid id, [FromBody] ProductModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = ToCreateUpdateDto(model);
                // Put (sửa) cho phép đổi Status; Post (tạo mới) để service đặt
                // giá trị mặc định — giữ nguyên behavior cũ của 2 endpoint.
                dto.Status = model.Status;
                var result = await _productAppService.UpdateAsync(id, dto);
                return Ok(result);
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái bán của món (Active &lt;-&gt; Inactive) bằng 1
        /// click ở trang danh sách sản phẩm admin — dùng khi món hết hàng
        /// cần ngưng bán tạm thời và mở lại khi có hàng.
        /// </summary>
        /// <param name="id">Id sản phẩm cần đổi trạng thái.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(AuthorizationPolicyNames.UpdateProductPolicy)]
        [HttpPost("{id}/toggle-availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDto>> ToggleAvailability(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = await _productAppService.ToggleAvailabilityAsync(id);
                return Ok(dto);
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Item 146: lưu thứ tự hiển thị món do admin kéo-thả thủ công ở
        /// trang /products. Body: { "ids": [guid, ...] } theo đúng thứ tự
        /// hiển thị mong muốn.
        /// </summary>
        /// <param name="model">Danh sách id sản phẩm theo thứ tự hiển thị.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(AuthorizationPolicyNames.UpdateProductPolicy)]
        [HttpPost("reorder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Reorder([FromBody] ReorderProductsModel model, CancellationToken cancellationToken = default)
        {
            if (model.Ids == null || model.Ids.Count == 0)
            {
                return BadRequest("Danh sách id trống.");
            }

            await _productAppService.SaveDisplayOrderAsync(model.Ids);
            return Ok();
        }

        /// <summary>
        /// Item 148: lịch sử thay đổi của 1 sản phẩm ("ai sửa gì, khi nào"),
        /// mới nhất lên trước. Chỉ Admin.
        /// </summary>
        /// <param name="id">Id sản phẩm.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [HttpGet("{id}/change-log")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetChangeLog(Guid id, CancellationToken cancellationToken = default)
        {
            return Ok(await _productAppService.GetChangeLogAsync(id));
        }

        /// <summary>Xóa sản phẩm (Admin, quyền DeleteProductPolicy). Không tìm thấy → 404.</summary>
        /// <param name="id">Id sản phẩm cần xóa.</param>
        [Authorize(AuthorizationPolicyNames.DeleteProductPolicy)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _productAppService.DeleteAsync(id);
                return Ok();
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Public: danh sách id sản phẩm xếp theo số lượng đã bán — phục vụ
        /// kiểu sắp xếp "Bán chạy" ở shop. Chỉ trả id (không lộ doanh thu).
        /// </summary>
        /// <param name="top">Số món lấy, mặc định 100.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpGet("best-sellers")]
        public async Task<ActionResult> GetBestSellers(
            [FromQuery] int top = 100,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetBestSellerProductIdsAsync(top));
        }

        /// <summary>
        /// Public: tổng số lượng đã bán của từng món (mọi thời gian, trừ đơn
        /// đã hủy). Dùng cho dòng "Đã bán N suất" (social proof) trên thẻ
        /// món ở shop.
        /// </summary>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpGet("sold-quantities")]
        public async Task<ActionResult> GetSoldQuantities(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetSoldQuantitiesAsync());
        }

        /// <summary>
        /// Public: trạng thái suất bán còn lại trong ngày của các món giới
        /// hạn số lượng ("Còn N suất" — item 165). Không cần đăng nhập — số
        /// lượng đã bán không phải dữ liệu nhạy cảm.
        /// </summary>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpGet("daily-remaining")]
        public async Task<ActionResult> GetDailyRemaining(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _productAppService.GetDailyRemainingAsync());
        }

        /// <summary>
        /// Public: điểm đánh giá tổng hợp theo từng món {ProductId,
        /// AverageRating, ReviewCount}, tính từ các đánh giá không bị ẩn.
        /// </summary>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpGet("ratings")]
        public async Task<ActionResult> GetRatings(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _productAppService.GetProductRatingsAsync());
        }

        /// <summary>
        /// Item 166: upload 1 ảnh sản phẩm (admin). Client tự nén ảnh trước
        /// khi gửi; endpoint này lưu file xuống disk và trả URL. Form sản
        /// phẩm thu thập các URL rồi lưu vào món qua endpoint PUT.
        /// </summary>
        /// <param name="file">File ảnh từ form (jpg/jpeg/png/webp/gif, tối đa 5 MB).</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(AuthorizationPolicyNames.AddProductPolicy)]
        [HttpPost("images/upload")]
        [RequestSizeLimit(MaxProductImageBytes)]
        public async Task<ActionResult> UploadProductImage(
            [FromForm] IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn một file ảnh." });
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Định dạng ảnh không hợp lệ. Chỉ nhận: jpg, jpeg, png, webp, gif."
                });
            }

            // bug-hunt 2026-10-19: kiểm tra chữ ký file (magic bytes) — đuôi
            // ".jpg" có thể là file thực thi/script đổi tên; chỉ nhận nội
            // dung ảnh thật (JPEG/PNG/GIF/WebP).
            if (!await ImageUploadValidator.HasValidImageSignatureAsync(file, extension, cancellationToken))
            {
                return BadRequest(new
                {
                    message = "File không phải ảnh hợp lệ (nội dung không khớp định dạng ảnh)."
                });
            }

            if (file.Length > MaxProductImageBytes)
            {
                return BadRequest(new { message = "File ảnh tối đa 5 MB." });
            }

            var folder = GetProductImageUploadFolder();
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(folder, fileName);
            await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return Ok(new { url = $"/uploads/products/{fileName}" });
        }

        /// <summary>
        /// Xóa file ảnh sản phẩm khỏi disk. Client gọi khi user bấm nút Xóa ảnh.
        /// </summary>
        [Authorize(AuthorizationPolicyNames.DeleteProductPolicy)]
        [HttpDelete("images")]
        public IActionResult DeleteProductImage([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new { message = "Thiếu đường dẫn ảnh." });
            }

            // Chỉ cho phép xóa file trong thư mục uploads/products để tránh path traversal.
            var fileName = Path.GetFileName(url);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { message = "Đường dẫn ảnh không hợp lệ." });
            }

            var folder = GetProductImageUploadFolder();
            var filePath = Path.Combine(folder, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            return Ok(new { message = "Đã xóa ảnh." });
        }

        /// <summary>Xuất toàn bộ danh sách sản phẩm ra file PDF (tối đa 1000 món).</summary>
        [HttpGet("exportaspdf")]
        public async Task<IActionResult> ExportAsPdf(CancellationToken cancellationToken = default)
        {
            var models = await GetAllProductModels(cancellationToken);

            var template = Path.Combine(Environment.CurrentDirectory, $"Templates/ProductList.cshtml");
            var html = await _htmlGenerator.GenerateAsync(template, models);
            var pdf = await _pdfConverter.ConvertAsync(html);

            return File(pdf, MediaTypeNames.Application.Octet, "Products.pdf");
        }

        /// <summary>Xuất toàn bộ danh sách sản phẩm ra file CSV (tối đa 1000 món).</summary>
        [HttpGet("exportascsv")]
        public async Task<IActionResult> ExportAsCsv(CancellationToken cancellationToken = default)
        {
            var models = await GetAllProductModels(cancellationToken);
            using var stream = new MemoryStream();
            _productCsvWriter.Write(models, stream);
            return File(stream.ToArray(), MediaTypeNames.Application.Octet, "Products.csv");
        }

        /// <summary>
        /// Import sản phẩm từ file CSV vào DB: mỗi dòng được ánh xạ sang
        /// <see cref="CreateUpdateProductDto"/> (dùng chung
        /// <see cref="ToCreateUpdateDto"/>) rồi tạo qua
        /// <see cref="IProductAppService.CreateAsync"/>. Dòng trùng mã với
        /// sản phẩm đã có — kể cả trùng trong cùng file — thì BỎ QUA (đếm
        /// skipped, không ghi đè); dòng lỗi validate/tạo thì ghi vào danh
        /// sách lỗi kèm số dòng + lý do, không làm sập cả batch. Trả về
        /// tổng hợp <see cref="ProductImportResultModel"/>.
        /// </summary>
        /// <param name="model">File CSV upload từ form.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [HttpPost("importcsv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ImportCsv([FromForm] UploadFileModel model, CancellationToken cancellationToken = default)
        {
            if (model?.FormFile == null || model.FormFile.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn file CSV để import." });
            }

            List<ProductModel> rows;
            try
            {
                using var stream = model.FormFile.OpenReadStream();
                rows = _productCsvReader.Read(stream).ToList();
            }
            catch (Exception ex)
            {
                // File hỏng/không đúng định dạng CSV: báo lỗi ở mức file
                // thay vì 500. Lúc này chưa dòng nào được ghi vào DB nên
                // không có trạng thái dở dang.
                return BadRequest(new { message = $"Không đọc được file CSV: {ex.Message}" });
            }

            var result = new ProductImportResultModel();

            // Số dòng trong file: dòng 1 là header, dữ liệu từ dòng 2.
            var rowNumber = 1;
            foreach (var row in rows)
            {
                rowNumber++;
                try
                {
                    await _productAppService.CreateAsync(ToCreateUpdateDto(row));
                    result.Created++;
                }
                catch (DuplicateProductCodeException)
                {
                    // Trùng mã: bỏ qua theo quy tắc import — không ghi đè
                    // món đang bán. CreateAsync tự kiểm tra mã trong DB
                    // nên mã trùng trong cùng file (dòng trước đã tạo
                    // thành công) cũng rơi vào nhánh này.
                    result.Skipped++;
                }
                catch (Exception ex)
                {
                    // Lỗi 1 dòng (mã/giá không hợp lệ, lỗi nghiệp vụ...)
                    // chỉ ghi lại rồi đi tiếp — không để 1 dòng xấu làm
                    // hỏng cả batch import.
                    result.Errors.Add(new ProductImportErrorModel
                    {
                        Row = rowNumber,
                        Code = row.Code,
                        Message = ex.Message
                    });
                }
            }

            return Ok(result);
        }

        private const string DefaultCurrency = "VND";

        /// <summary>
        /// Ánh xạ form sản phẩm (ProductModel) sang DTO nghiệp vụ. Dùng chung
        /// cho Post (tạo) và Put (sửa); chỉ Put gán thêm Status sau khi gọi.
        /// </summary>
        private static CreateUpdateProductDto ToCreateUpdateDto(ProductModel model)
        {
            return new CreateUpdateProductDto
            {
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Currency = string.IsNullOrWhiteSpace(model.Currency) ? DefaultCurrency : model.Currency,
                CostPrice = model.CostPrice,
                Tags = model.Tags,
                Category = model.Category,
                AllergenInfo = model.AllergenInfo,
                IsNew = model.IsNew,
                IsSignature = model.IsSignature,
                AvailableFrom = model.AvailableFrom,
                AvailableTo = model.AvailableTo,
                BranchId = model.BranchId,
                Unit = model.Unit,
                AvailableHoursJson = model.AvailableHoursJson,
                DailyLimit = model.DailyLimit,
                ImageUrlsJson = model.ImageUrlsJson
            };
        }

        /// <summary>
        /// Thư mục lưu ảnh sản phẩm: wwwroot/uploads/products. Dùng chung
        /// cho upload và xóa file ảnh.
        /// </summary>
        private string GetProductImageUploadFolder() =>
            Path.Combine(
                _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
                "uploads",
                "products");

        /// <summary>
        /// Lấy toàn bộ (tối đa 1000 món) danh sách sản phẩm dạng ProductModel
        /// để xuất PDF/CSV. Dùng chung cho ExportAsPdf và ExportAsCsv.
        /// </summary>
        /// <param name="cancellationToken">Token hủy request.</param>
        private async Task<List<ProductModel>> GetAllProductModels(CancellationToken cancellationToken)
        {
            var paged = await _productAppService.GetListAsync(new GetProductsInput
            {
                MaxResultCount = 1000,
                SkipCount = 0
            });
            return paged.Items.Select(x => new ProductModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Currency = x.Currency,
                Status = x.Status,
                CostPrice = x.CostPrice
            }).ToList();
        }

        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated
        // (tránh cấp phát params array + boxing mỗi lần log).

        /// <summary>Log khi bắt đầu lấy danh sách sản phẩm (endpoint public).</summary>
        [LoggerMessage(Level = LogLevel.Information, Message = "Getting products.")]
        private static partial void LogGettingProducts(ILogger logger);
    }
}
