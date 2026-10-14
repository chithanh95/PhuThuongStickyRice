using PhuThuongStickyRice.Application;
using PhuThuongStickyRice.Application.AuditLogEntries.Queries;
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductAppService _productAppService;
        private readonly Dispatcher _dispatcher;
        private readonly ILogger _logger;
        private readonly IHtmlGenerator _htmlGenerator;
        private readonly IPdfConverter _pdfConverter;
        private readonly ICsvWriter<ProductModel> _productCsvWriter;
        private readonly ICsvReader<ProductModel> _productCsvReader;

        public ProductsController(IProductAppService productAppService,
            Dispatcher dispatcher,
            ILogger<ProductsController> logger,
            IHtmlGenerator htmlGenerator,
            IPdfConverter pdfConverter,
            ICsvWriter<ProductModel> productCsvWriter,
            ICsvReader<ProductModel> productCsvReader)
        {
            _productAppService = productAppService;
            _dispatcher = dispatcher;
            _logger = logger;
            _htmlGenerator = htmlGenerator;
            _pdfConverter = pdfConverter;
            _productCsvWriter = productCsvWriter;
            _productCsvReader = productCsvReader;
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
                    Currency = string.IsNullOrWhiteSpace(model.Currency) ? "VND" : model.Currency
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
                    Status = model.Status
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

        [Authorize(AuthorizationPolicyNames.GetProductAuditLogsPolicy)]
        [HttpGet("{id}/auditlogs")]
        public async Task<ActionResult> GetAuditLogs(Guid id, CancellationToken cancellationToken = default)
        {
            // NOTE: audit log reads still go through the legacy Dispatcher; it will be
            // migrated to an ABP Application Service together with the AuditLogEntries feature.
            var logs = await _dispatcher.DispatchAsync(new GetAuditEntriesQuery { ObjectId = id.ToString() }, cancellationToken);

            var entries = new List<object>();
            ProductSnapshot previous = null;
            foreach (var log in logs.OrderBy(x => x.CreatedDateTime))
            {
                var data = ProductSnapshot.FromJson(log.Log);
                var highLight = new Dictionary<string, bool>
                {
                    ["Code"] = previous != null && data.Code != previous.Code,
                    ["Name"] = previous != null && data.Name != previous.Name,
                    ["Description"] = previous != null && data.Description != previous.Description,
                };

                entries.Add(new
                {
                    log.Id,
                    log.UserName,
                    Action = log.Action.Replace("_PRODUCT", string.Empty),
                    log.CreatedDateTime,
                    data,
                    highLight,
                });

                previous = data;
            }

            return Ok(entries.OrderByDescending(x => ((dynamic)x).CreatedDateTime));
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

        /// <summary>
        /// Tolerant snapshot of a product audit entry. Handles both the legacy flat
        /// JSON shape and the new shape where Code/Price are value objects.
        /// </summary>
        private sealed class ProductSnapshot
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

            public static ProductSnapshot FromJson(string json)
            {
                var snapshot = new ProductSnapshot();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return snapshot;
                }

                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    snapshot.Code = GetString(root, "Code");
                    snapshot.Name = GetString(root, "Name");
                    snapshot.Description = GetString(root, "Description");
                }
                catch (JsonException)
                {
                    // keep empty snapshot
                }

                return snapshot;
            }

            private static string GetString(JsonElement root, string propertyName)
            {
                if (!TryGetPropertyCaseInsensitive(root, propertyName, out var prop))
                {
                    return null;
                }

                if (prop.ValueKind == JsonValueKind.Object
                    && prop.TryGetProperty("Value", out var nested)
                    && nested.ValueKind == JsonValueKind.String)
                {
                    return nested.GetString();
                }

                return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
            }

            private static bool TryGetPropertyCaseInsensitive(JsonElement root, string propertyName, out JsonElement value)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = prop.Value;
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }
    }
}
