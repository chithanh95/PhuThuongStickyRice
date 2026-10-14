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
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductAppService _productAppService;
        private readonly ILogger _logger;
        private readonly IHtmlGenerator _htmlGenerator;
        private readonly IPdfConverter _pdfConverter;
        private readonly ICsvWriter<ProductModel> _productCsvWriter;
        private readonly ICsvReader<ProductModel> _productCsvReader;
        private readonly PhuThuongStickyRice.Application.Orders.IDashboardAppService _dashboardAppService;

        public ProductsController(IProductAppService productAppService,
            ILogger<ProductsController> logger,
            IHtmlGenerator htmlGenerator,
            IPdfConverter pdfConverter,
            ICsvWriter<ProductModel> productCsvWriter,
            ICsvReader<ProductModel> productCsvReader,
            PhuThuongStickyRice.Application.Orders.IDashboardAppService dashboardAppService)
        {
            _productAppService = productAppService;
            _logger = logger;
            _htmlGenerator = htmlGenerator;
            _pdfConverter = pdfConverter;
            _productCsvWriter = productCsvWriter;
            _productCsvReader = productCsvReader;
            _dashboardAppService = dashboardAppService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string searchTerm = null, [FromQuery] string status = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting products");
            var paged = await _productAppService.GetListAsync(new GetProductsInput
            {
                MaxResultCount = pageSize < 1 ? 20 : pageSize,
                SkipCount = (page < 1 ? 0 : page - 1) * (pageSize < 1 ? 20 : pageSize),
                SearchTerm = searchTerm,
                Status = status
            });
            return Ok(paged);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = await _productAppService.GetAsync(id);
                return Ok(dto);
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize(AuthorizationPolicyNames.AddProductPolicy)]
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDto>> Post([FromBody] ProductModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _productAppService.CreateAsync(new CreateUpdateProductDto
                {
                    Code = model.Code,
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    Currency = string.IsNullOrWhiteSpace(model.Currency) ? "VND" : model.Currency,
                    Tags = model.Tags,
                    Category = model.Category,
                    AllergenInfo = model.AllergenInfo,
                    IsNew = model.IsNew,
                    IsSignature = model.IsSignature,
                    AvailableFrom = model.AvailableFrom,
                    AvailableTo = model.AvailableTo
                });
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
                var dto = await _productAppService.UpdateAsync(id, new CreateUpdateProductDto
                {
                    Code = model.Code,
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    Currency = string.IsNullOrWhiteSpace(model.Currency) ? "VND" : model.Currency,
                    Status = model.Status,
                    Tags = model.Tags,
                    Category = model.Category,
                    AllergenInfo = model.AllergenInfo,
                    IsNew = model.IsNew,
                    IsSignature = model.IsSignature,
                    AvailableFrom = model.AvailableFrom,
                    AvailableTo = model.AvailableTo
                });
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
        /// One-click sold-out toggle (Active &lt;-&gt; Inactive) for the
        /// admin product list.
        /// </summary>
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
        /// Public: product ids ordered by quantity sold, for the shop's
        /// "Bán chạy" sort. Returns ids only (no revenue data).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("best-sellers")]
        public async Task<ActionResult> GetBestSellers(
            [FromQuery] int top = 100,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetBestSellerProductIdsAsync(top));
        }

        [HttpGet("exportaspdf")]
        public async Task<IActionResult> ExportAsPdf(CancellationToken cancellationToken = default)
        {
            var models = await GetAllProductModels(cancellationToken);

            var template = Path.Combine(Environment.CurrentDirectory, $"Templates/ProductList.cshtml");
            var html = await _htmlGenerator.GenerateAsync(template, models);
            var pdf = await _pdfConverter.ConvertAsync(html);

            return File(pdf, MediaTypeNames.Application.Octet, "Products.pdf");
        }

        [HttpGet("exportascsv")]
        public async Task<IActionResult> ExportAsCsv(CancellationToken cancellationToken = default)
        {
            var models = await GetAllProductModels(cancellationToken);
            using var stream = new MemoryStream();
            _productCsvWriter.Write(models, stream);
            return File(stream.ToArray(), MediaTypeNames.Application.Octet, "Products.csv");
        }

        [HttpPost("importcsv")]
        public IActionResult ImportCsv([FromForm] UploadFileModel model)
        {
            using var stream = model.FormFile.OpenReadStream();
            var products = _productCsvReader.Read(stream);

            // TODO: import to database
            return Ok(products);
        }

        private async Task<List<ProductModel>> GetAllProductModels(CancellationToken cancellationToken)
        {
            var paged = await _productAppService.GetListAsync(new GetProductsInput
            {
                MaxResultCount = int.MaxValue,
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
                Status = x.Status
            }).ToList();
        }
    }
}
