using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Wave3.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Wave3
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

        public TableQrAppService(
            IRepository<TableQr, Guid> tableQrs,
            IAsyncQueryableExecuter asyncExecuter,
            IShopSettingAppService shopSettings)
        {
            _tableQrs = tableQrs;
            _asyncExecuter = asyncExecuter;
            _shopSettings = shopSettings;
        }

        public async Task<List<TableQrDto>> GetListAsync()
        {
            var queryable = await _tableQrs.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.Label));
            return items.Select(Map).ToList();
        }

        public async Task<TableQrGeneratedDto> GenerateAsync(CreateTableQrDto input)
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

        public async Task DeleteAsync(Guid id)
        {
            await _tableQrs.DeleteAsync(id, autoSave: true);
        }

        public async Task<byte[]> RenderPngAsync(Guid id)
        {
            var row = await _tableQrs.GetAsync(id);
            return RenderPng(row.EncodedUrl);
        }

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
