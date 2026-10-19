using PhuThuongStickyRice.Application.Inventory.DTOs;
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

namespace PhuThuongStickyRice.Application.Inventory
{
    public interface IInventoryAppService : IApplicationService
    {
        /// <summary>Admin: saves the end-of-day counts for a date (replaces existing lines).</summary>
        Task SaveAsync(SaveDailyInventoryInput input);

        /// <summary>Admin: inventory history for the last N days, newest first.</summary>
        Task<List<DailyInventoryDayDto>> GetHistoryAsync(int days = 30);
    }

    /// <summary>
    /// End-of-day inventory counts (kiểm kê cuối ngày, item 87).
    /// Saving a date replaces all its lines, so recounting is just a re-save.
    /// History shows the per-product difference vs the previous count.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class InventoryAppService : ApplicationService, IInventoryAppService
    {
        private readonly IRepository<DailyInventory, Guid> _inventories;
        private readonly IRepository<Product, Guid> _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public InventoryAppService(
            IRepository<DailyInventory, Guid> inventories,
            IRepository<Product, Guid> products,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _inventories = inventories;
            _products = products;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Lưu số liệu kiểm kê cuối ngày (xem doc ở interface): ghi đè toàn
        /// bộ dòng của ngày — kiểm kê lại chỉ cần lưu lại, không cần xóa tay.
        /// Không nhận ngày trong tương lai.
        /// </summary>
        /// <param name="input">Ngày kiểm kê và danh sách số lượng còn lại theo món.</param>
        public virtual async Task SaveAsync(SaveDailyInventoryInput input)
        {
            var date = ValidateAndGetDate(input);
            var productNamesById = await LoadProductNamesAsync(input.Lines);
            ValidateLines(input.Lines, productNamesById);

            // Replace-all for the date: delete existing lines, then insert.
            await DeleteExistingLinesAsync(date);
            await InsertLinesAsync(date, input.Lines, productNamesById);
        }

        /// <summary>Kiểm tra đầu vào hợp lệ và trả về ngày kiểm kê (không nhận ngày trong tương lai).</summary>
        private static DateTime ValidateAndGetDate(SaveDailyInventoryInput input)
        {
            if (input == null || input.Lines == null)
            {
                throw new Domain.Exceptions.DomainException("Dữ liệu kiểm kê không hợp lệ.");
            }

            var date = input.Date.Date;
            if (date > VietnamTime.Today().AddDays(1))
            {
                throw new Domain.Exceptions.DomainException("Không thể kiểm kê cho ngày trong tương lai.");
            }

            return date;
        }

        /// <summary>Nạp tên sản phẩm cho các dòng kiểm kê (dùng khi validate và khi ghi ProductName).</summary>
        private async Task<Dictionary<Guid, string>> LoadProductNamesAsync(List<InventoryLineInput> lines)
        {
            var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await _asyncExecuter.ToListAsync(
                (await _products.GetQueryableAsync()).Where(p => productIds.Contains(p.Id)));
            return products.ToDictionary(p => p.Id, p => p.Name);
        }

        /// <summary>Kiểm tra số lượng không âm, sản phẩm tồn tại và không trùng món cho từng dòng kiểm kê.</summary>
        private static void ValidateLines(List<InventoryLineInput> lines, Dictionary<Guid, string> productNamesById)
        {
            var seen = new HashSet<Guid>();
            foreach (var line in lines)
            {
                if (line.RemainingQty < 0)
                {
                    throw new Domain.Exceptions.DomainException("Số lượng còn lại không được âm.");
                }

                if (!productNamesById.ContainsKey(line.ProductId))
                {
                    throw new Domain.Exceptions.DomainException("Sản phẩm không tồn tại.");
                }

                // Mỗi món chỉ 1 dòng/ngày: trùng ProductId sẽ insert 2 row cùng
                // (Date, ProductId) làm lệch báo cáo tồn kho → từ chối ngay.
                if (!seen.Add(line.ProductId))
                {
                    throw new Domain.Exceptions.DomainException(
                        $"Sản phẩm '{productNamesById[line.ProductId]}' bị nhập trùng 2 dòng. Mỗi món chỉ nhập 1 dòng.");
                }
            }
        }

        /// <summary>Xóa toàn bộ dòng kiểm kê đã lưu của một ngày (để ghi đè khi kiểm kê lại).</summary>
        private async Task DeleteExistingLinesAsync(DateTime date)
        {
            var existing = await _asyncExecuter.ToListAsync(
                (await _inventories.GetQueryableAsync()).Where(x => x.Date == date));
            foreach (var e in existing)
            {
                await _inventories.DeleteAsync(e);
            }
        }

        /// <summary>Chèn các dòng kiểm kê mới cho một ngày.</summary>
        private async Task InsertLinesAsync(
            DateTime date, List<InventoryLineInput> lines, Dictionary<Guid, string> productNamesById)
        {
            var byUser = CurrentUser?.UserName;
            foreach (var line in lines)
            {
                await _inventories.InsertAsync(new DailyInventory
                {
                    Date = date,
                    ProductId = line.ProductId,
                    ProductName = productNamesById[line.ProductId],
                    RemainingQty = line.RemainingQty,
                    Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
                    CreatedBy = byUser,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Lịch sử kiểm kê N ngày gần nhất, mới nhất trước (xem doc ở
        /// interface). Mỗi dòng kèm PreviousRemainingQty so với lần kiểm kê
        /// trước của cùng món — tính bằng ngày VN (không phải giờ server) để
        /// biên ngày đúng khi server chạy UTC (Docker/K8s).
        /// </summary>
        /// <param name="days">Số ngày lấy, 1–90 (mặc định 30).</param>
        /// <returns>Danh sách ngày kiểm kê kèm chênh lệch so với lần trước.</returns>
        public virtual async Task<List<DailyInventoryDayDto>> GetHistoryAsync(int days = 30)
        {
            if (days < 1) days = 1;
            if (days > 90) days = 90;

            // Perf: chỉ nạp (days+1) ngày gần nhất trong SQL thay vì toàn bộ bảng.
            // Ngày thừa (cũ nhất) chỉ dùng để tính PreviousRemainingQty cho ngày
            // đầu của cửa sổ, kết quả trả về giống hệt trước đây.
            // Bug fix (bug-hunt run 14:25): dùng VietnamTime.Today() thay vì
            // DateTime.Today (giờ server). Nếu server chạy UTC (Docker/K8s),
            // khung 00:00–06:59 giờ VN lệch 1 ngày → cutoff biên sai,
            // PreviousRemainingQty của ngày đầu cửa sổ tính nhầm.
            var cutoff = VietnamTime.Today().AddDays(-days);
            var all = await _asyncExecuter.ToListAsync(
                (await _inventories.GetQueryableAsync())
                    .Where(x => x.Date >= cutoff)
                    .OrderByDescending(x => x.Date)
                    .ThenBy(x => x.ProductName));

            // Previous count per product: iterate oldest → newest, keeping
            // the last seen qty per product; display newest first.
            var lastSeenByProduct = new Dictionary<Guid, int>();

            var ascending = all.GroupBy(x => x.Date).OrderBy(g => g.Key).ToList();
            var daysNewestFirst = new List<DailyInventoryDayDto>();
            foreach (var group in ascending)
            {
                var day = new DailyInventoryDayDto { Date = group.Key };
                foreach (var line in group.OrderBy(x => x.ProductName))
                {
                    var hasPrev = lastSeenByProduct.TryGetValue(line.ProductId, out var prev);
                    day.Lines.Add(new DailyInventoryLineDto
                    {
                        ProductId = line.ProductId,
                        ProductName = line.ProductName,
                        RemainingQty = line.RemainingQty,
                        Note = line.Note,
                        PreviousRemainingQty = hasPrev ? prev : (int?)null,
                        CreatedBy = line.CreatedBy,
                        CreatedAt = line.CreatedAt
                    });
                    lastSeenByProduct[line.ProductId] = line.RemainingQty;
                }

                daysNewestFirst.Add(day);
            }

            daysNewestFirst.Reverse();
            return daysNewestFirst.Take(days).ToList();
        }
    }
}
