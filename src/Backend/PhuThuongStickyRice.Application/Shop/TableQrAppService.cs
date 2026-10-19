using PhuThuongStickyRice.Application.Shop;

using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Item 95: table QR codes encoding {WebBaseUrl}/shop?ban={Label}.
    /// PNG generation is fully offline via QRCoder.
    /// </summary>
    public interface ITableQrAppService : IApplicationService
    {
        Task<List<TableQrDto>> GetListAsync();

        Task<TableQrGeneratedDto> GenerateAsync(CreateTableQrDto input);

        Task DeleteAsync(Guid id);

        /// <summary>Re-renders the PNG for a saved QR (download button).</summary>
        Task<byte[]> RenderPngAsync(Guid id);
    }

    [Authorize(Roles = "Admin")]
    public class TableQrAppService : ApplicationService, ITableQrAppService
    {
        private readonly IRepository<TableQr, Guid> _tableQrs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IMemoryCache _cache;

        public TableQrAppService(
            IRepository<TableQr, Guid> tableQrs,
            IAsyncQueryableExecuter asyncExecuter,
            IShopSettingAppService shopSettings,
            IMemoryCache cache)
        {
            _tableQrs = tableQrs;
            _asyncExecuter = asyncExecuter;
            _shopSettings = shopSettings;
            _cache = cache;
        }

        public virtual async Task<List<TableQrDto>> GetListAsync()
        {
            var queryable = await _tableQrs.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.Label));
            return items.Select(Map).ToList();
        }

        public virtual async Task<TableQrGeneratedDto> GenerateAsync(CreateTableQrDto input)
        {
            var label = input.Label.Trim();
            var url = await BuildUrlAsync(label);

            var queryable = await _tableQrs.GetQueryableAsync();
            var existing = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Label == label));

            TableQr row;
            if (existing == null)
            {
                row = new TableQr
                {
                    Label = label,
                    EncodedUrl = url,
                    CreatedAt = DateTime.UtcNow
                };
                await _tableQrs.InsertAsync(row, autoSave: true);
            }
            else
            {
                existing.EncodedUrl = url;
                await _tableQrs.UpdateAsync(existing, autoSave: true);
                row = existing;
            }

            // URL có thể đổi khi WebBaseUrl đổi — PNG cache theo id nên phải
            // xóa entry cũ để lần tải sau render lại từ EncodedUrl mới.
            _cache.Remove(PngCacheKey(row.Id));

            var png = RenderPng(url);
            return new TableQrGeneratedDto
            {
                Id = row.Id,
                Label = row.Label,
                EncodedUrl = row.EncodedUrl,
                CreatedAt = row.CreatedAt,
                PngBase64 = Convert.ToBase64String(png)
            };
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            _cache.Remove(PngCacheKey(id));
            await _tableQrs.DeleteAsync(id, autoSave: true);
        }

        public virtual async Task<byte[]> RenderPngAsync(Guid id)
        {
            // perf-hunt 2026-10-19: the anonymous /png download re-rendered the
            // QR on every request (CPU-bound, no input change). PNG bytes are a
            // pure function of the saved EncodedUrl, so cache them; the entry is
            // evicted whenever the row is regenerated or deleted below.
            return await _cache.GetOrCreateAsync(PngCacheKey(id), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                var row = await _tableQrs.GetAsync(id);
                return RenderPng(row.EncodedUrl);
            });
        }

        private static string PngCacheKey(Guid id) => $"tableqr:png:{id}";

        private async Task<string> BuildUrlAsync(string label)
        {
            var settings = await _shopSettings.GetAsync();
            var baseUrl = (settings.WebBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new Domain.Exceptions.DomainException(
                    "Chưa cấu hình địa chỉ web (WebBaseUrl) trong Cài đặt bán hàng. Vui lòng cấu hình trước khi tạo QR bàn.");
            }

            return $"{baseUrl}/shop?ban={Uri.EscapeDataString(label)}";
        }

        private static TableQrDto Map(TableQr t) => new TableQrDto
        {
            Id = t.Id,
            Label = t.Label,
            EncodedUrl = t.EncodedUrl,
            CreatedAt = t.CreatedAt
        };

        /// <summary>Renders a QR code as PNG bytes (offline, QRCoder).</summary>
        public static byte[] RenderPng(string content)
        {
            using var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            return new PngByteQRCode(data).GetGraphic(20);
        }
    }
}
