
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Common
{
    public interface IFeatureFlagAppService : IApplicationService
    {
        /// <summary>
        /// Admin: toàn bộ cờ tính năng kèm metadata. Lần đầu gọi sẽ tự tạo
        /// các cờ mặc định còn thiếu.
        /// </summary>
        /// <returns>Danh sách cờ tính năng.</returns>
        Task<List<FeatureFlagDto>> GetListAsync();

        /// <summary>
        /// Admin: bật/tắt 1 cờ tính năng (đồng thời xóa cache bản đồ public
        /// để shop thấy ngay).
        /// </summary>
        /// <param name="key">Key của cờ tính năng.</param>
        /// <param name="enabled">true: bật, false: tắt.</param>
        /// <returns>Cờ tính năng sau khi cập nhật.</returns>
        Task<FeatureFlagDto> SetEnabledAsync(string key, bool enabled);

        /// <summary>
        /// Public: bản đồ key → trạng thái bật/tắt để UI ẩn/hiện tính năng.
        /// Cache 5 phút; không cần đăng nhập vì trang shop gọi lúc tải trang.
        /// </summary>
        /// <returns>Dictionary key cờ → true nếu đang bật.</returns>
        Task<Dictionary<string, bool>> GetEnabledMapAsync();

        /// <summary>
        /// Nội bộ: true khi cờ tồn tại và đang bật.
        /// </summary>
        /// <param name="key">Key của cờ tính năng.</param>
        /// <returns>true nếu cờ tồn tại và đang bật.</returns>
        Task<bool> IsEnabledAsync(string key);
    }

    /// <summary>
    /// Cờ tính năng (feature flags) của shop: admin bật/tắt các tính năng tùy
    /// chọn (vòng quay may mắn, gói quà, giới thiệu bạn bè, popup khuyến mãi)
    /// tại /admin/tinh-nang mà không cần deploy code. Bản đồ cờ public được
    /// cache 5 phút để trang shop không query DB mỗi lần tải.
    /// </summary>
    public class FeatureFlagAppService : ApplicationService, IFeatureFlagAppService
    {
        private readonly IRepository<FeatureFlag, Guid> _flags;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        private static readonly (string Key, string Name, string Description)[] Defaults =
        {
            (FeatureFlag.LuckySpin, "Vòng quay may mắn", "Mini-game quay thưởng sau khi đặt đơn thành công."),
            (FeatureFlag.GiftWrap, "Gói quà", "Cho phép khách chọn gói quà (tính phí) ở bước thanh toán."),
            (FeatureFlag.Referrals, "Giới thiệu bạn bè", "Nhập mã giới thiệu ở bước thanh toán để nhận điểm thưởng."),
            (FeatureFlag.PromoPopup, "Popup khuyến mãi", "Hiện popup khuyến mãi trên trang chủ (1 lần/phiên).")
        };

        /// <summary>Khởi tạo service quản lý cờ tính năng.</summary>
        /// <param name="flags">Repository bảng FeatureFlag.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn bất đồng bộ.</param>
        /// <param name="catalogCache">Cache public (bản đồ cờ), invalidate khi admin đổi cờ.</param>
        public FeatureFlagAppService(
            IRepository<FeatureFlag, Guid> flags,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _flags = flags;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Admin: danh sách toàn bộ cờ kèm metadata (xem doc ở interface).
        /// Tự seed các cờ mặc định (đang BẬT) nếu bảng còn trống.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<FeatureFlagDto>> GetListAsync()
        {
            await EnsureDefaultsAsync();
            var queryable = await _flags.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(queryable.OrderBy(x => x.Key));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: bật/tắt một cờ (xem doc ở interface). Xóa cache bản đồ cờ
        /// public ngay để thay đổi có hiệu lực tức thì trên shop.
        /// </summary>
        /// <param name="key">Key cờ (vd "lucky-spin").</param>
        /// <param name="enabled">Bật hay tắt.</param>
        /// <returns>Cờ sau khi cập nhật.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<FeatureFlagDto> SetEnabledAsync(string key, bool enabled)
        {
            await EnsureDefaultsAsync();
            var entity = await FindOrThrowAsync(key);
            entity.IsEnabled = enabled;
            await _flags.UpdateAsync(entity, autoSave: true);
            // Perf: bust the public flags cache so the toggle takes effect immediately.
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        /// <summary>
        /// Public: bản đồ key → trạng thái bật/tắt để UI gating (xem doc ở
        /// interface). Cache 5 phút; bị invalidate bởi <see cref="SetEnabledAsync"/>.
        /// </summary>
        /// <returns>Dictionary key cờ → true nếu đang bật.</returns>
        [AllowAnonymous]
        public virtual async Task<Dictionary<string, bool>> GetEnabledMapAsync()
        {
            // Perf: the public map is fetched by the storefront on page load.
            // Cache 5 minutes; invalidated by SetEnabledAsync above.
            return await _catalogCache.GetOrAddAsync("feature-flags:enabled-map", async () =>
            {
                await EnsureDefaultsAsync();
                var queryable = await _flags.GetQueryableAsync();
                var entities = await _asyncExecuter.ToListAsync(queryable);
                return entities.ToDictionary(x => x.Key, x => x.IsEnabled);
            });
        }

        /// <summary>
        /// Nội bộ: cờ có đang bật không (xem doc ở interface). Cờ chưa seed
        /// thì mặc định BẬT để thiếu dữ liệu seed không bao giờ làm gãy tính
        /// năng đang chạy.
        /// </summary>
        /// <param name="key">Key cờ cần kiểm tra.</param>
        /// <returns>true khi cờ tồn tại và đang bật, hoặc cờ chưa tồn tại.</returns>
        public virtual async Task<bool> IsEnabledAsync(string key)
        {
            // Perf: serve from the cached map instead of a per-call DB query.
            var map = await GetEnabledMapAsync();
            // Unknown flags default to enabled so a missing seed row can
            // never break an existing feature.
            return !map.TryGetValue(key, out var enabled) || enabled;
        }

        /// <summary>
        /// Seed các cờ mặc định còn thiếu. Gom tất cả cờ thiếu rồi
        /// <c>InsertManyAsync</c> một lần (1 SaveChanges) thay vì
        /// <c>InsertAsync(autoSave: true)</c> từng cờ trong vòng lặp —
        /// vừa ít roundtrip DB vừa nguyên tử. Kết quả cuối giống hệt bản cũ:
        /// các cờ được commit ngay để query phía dưới thấy được.
        /// </summary>
        private async Task EnsureDefaultsAsync()
        {
            var queryable = await _flags.GetQueryableAsync();
            var existing = (await _asyncExecuter.ToListAsync(queryable))
                .Select(x => x.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missing = Defaults
                .Where(d => !existing.Contains(d.Key))
                .Select(d => new FeatureFlag
                {
                    Key = d.Key,
                    Name = d.Name,
                    Description = d.Description,
                    IsEnabled = true
                })
                .ToList();

            if (missing.Count > 0)
            {
                await _flags.InsertManyAsync(missing, autoSave: true);
            }
        }

        private async Task<FeatureFlag> FindOrThrowAsync(string key)
        {
            var queryable = await _flags.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Key == key));
            if (entity == null)
            {
                throw new Domain.Exceptions.DomainException($"Feature flag '{key}' không tồn tại.");
            }

            return entity;
        }

        private static FeatureFlagDto MapToDto(FeatureFlag entity)
        {
            return new FeatureFlagDto
            {
                Id = entity.Id,
                Key = entity.Key,
                Name = entity.Name,
                IsEnabled = entity.IsEnabled,
                Description = entity.Description
            };
        }
    }
}
