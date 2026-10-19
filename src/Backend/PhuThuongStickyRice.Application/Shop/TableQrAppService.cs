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

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Item 95: mã QR cho từng bàn, mã hóa URL {WebBaseUrl}/shop?ban={Label}.
    /// Khách quét QR ở bàn thì vào shop với tham số bàn để đơn gắn đúng bàn.
    /// Render PNG hoàn toàn offline bằng QRCoder (không gọi dịch vụ ngoài).
    /// </summary>
    public interface ITableQrAppService : IApplicationService
    {
        /// <summary>Admin: liệt kê toàn bộ QR bàn, sắp theo nhãn bàn.</summary>
        /// <returns>Danh sách QR bàn.</returns>
        Task<List<TableQrDto>> GetListAsync();

        /// <summary>
        /// Admin: tạo QR cho 1 nhãn bàn. Nhãn đã tồn tại thì ghi đè URL mới
        /// (URL phụ thuộc WebBaseUrl, đổi WebBaseUrl thì URL cũ hết hiệu lực).
        /// </summary>
        /// <param name="input">Nhãn bàn (vd "Bàn 1").</param>
        /// <returns>Thông tin QR vừa tạo/cập nhật kèm PNG base64.</returns>
        Task<TableQrGeneratedDto> GenerateAsync(CreateTableQrDto input);

        /// <summary>Admin: xóa QR bàn và xóa luôn PNG đang cache.</summary>
        /// <param name="id">Id QR bàn cần xóa.</param>
        Task DeleteAsync(Guid id);

        /// <summary>Render lại PNG cho QR đã lưu (nút tải xuống).</summary>
        /// <param name="id">Id QR bàn.</param>
        /// <returns>Byte PNG của mã QR.</returns>
        Task<byte[]> RenderPngAsync(Guid id);
    }

    /// <summary>
    /// Quản lý mã QR bàn cho quán (item 95). PNG của QR là hàm thuần túy theo
    /// EncodedUrl đã lưu nên được cache 24 giờ trong memory; mỗi lần tạo lại
    /// hoặc xóa QR đều xóa entry cache để lần tải sau render lại từ URL mới.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class TableQrAppService : ApplicationService, ITableQrAppService
    {
        /// <summary>Thời gian cache PNG QR bàn (giờ) — PNG chỉ đổi khi WebBaseUrl/nhãn đổi.</summary>
        private const int PngCacheHours = 24;

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

        /// <summary>
        /// Admin: liệt kê toàn bộ QR bàn, sắp theo nhãn bàn.
        /// </summary>
        /// <returns>Danh sách QR bàn.</returns>
        public virtual async Task<List<TableQrDto>> GetListAsync()
        {
            var queryable = await _tableQrs.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.Label));
            return items.Select(Map).ToList();
        }

        /// <summary>
        /// Admin: tạo QR cho 1 nhãn bàn (nhãn trùng thì cập nhật URL mới).
        /// URL phụ thuộc WebBaseUrl trong cài đặt — đổi WebBaseUrl thì phải
        /// tạo lại QR, nên sau khi lưu luôn xóa cache PNG theo id để lần tải
        /// sau render lại từ URL mới.
        /// </summary>
        /// <param name="input">Nhãn bàn (vd "Bàn 1").</param>
        /// <returns>Thông tin QR vừa tạo/cập nhật kèm PNG base64.</returns>
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

        /// <summary>
        /// Admin: xóa QR bàn và xóa luôn PNG đang cache để không còn tải được
        /// ảnh QR của bàn đã xóa.
        /// </summary>
        /// <param name="id">Id QR bàn cần xóa.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            _cache.Remove(PngCacheKey(id));
            await _tableQrs.DeleteAsync(id, autoSave: true);
        }

        /// <summary>
        /// Render lại PNG cho QR đã lưu (nút tải xuống). PNG được cache 24 giờ
        /// vì render QR tốn CPU mà bytes PNG chỉ phụ thuộc EncodedUrl đã lưu;
        /// cache bị xóa mỗi khi QR được tạo lại hoặc xóa.
        /// </summary>
        /// <param name="id">Id QR bàn.</param>
        /// <returns>Byte PNG của mã QR.</returns>
        public virtual async Task<byte[]> RenderPngAsync(Guid id)
        {
            // Render QR tốn CPU nhưng bytes PNG chỉ phụ thuộc EncodedUrl đã lưu
            // (không đổi theo request) nên cache lại; entry bị xóa mỗi khi QR
            // được tạo lại hoặc xóa ở các method trên.
            return await _cache.GetOrCreateAsync(PngCacheKey(id), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(PngCacheHours);
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

        private static TableQrDto Map(TableQr tableQr) => new TableQrDto
        {
            Id = tableQr.Id,
            Label = tableQr.Label,
            EncodedUrl = tableQr.EncodedUrl,
            CreatedAt = tableQr.CreatedAt
        };

        /// <summary>
        /// Render mã QR thành byte PNG (offline, dùng QRCoder).
        /// </summary>
        /// <param name="content">Nội dung mã hóa vào QR (URL của bàn).</param>
        /// <returns>Byte PNG của mã QR.</returns>
        public static byte[] RenderPng(string content)
        {
            using var generator = new QRCodeGenerator();
            // ECCLevel.M: mức sửa lỗi trung bình — QR vẫn đọc được khi ~15%
            // diện tích bị che/bẩn (thực tế tem dán bàn dễ bị dính nước/bụi).
            var qrCodeData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            // 20 pixel cho mỗi module QR — đủ nét để in tem dán bàn và điện
            // thoại quét nhanh, không cần ảnh lớn hơn gây tốn dung lượng.
            return new PngByteQRCode(qrCodeData).GetGraphic(20);
        }
    }
}
