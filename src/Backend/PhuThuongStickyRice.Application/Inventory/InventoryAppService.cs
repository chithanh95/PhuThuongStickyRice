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

        public virtual async Task SaveAsync(SaveDailyInventoryInput input)
        {
            if (input == null || input.Lines == null)
            {
                throw new Domain.Exceptions.DomainException("Dữ liệu kiểm kê không hợp lệ.");
            }

            var date = input.Date.Date;
            if (date > DateTime.UtcNow.AddHours(7).Date.AddDays(1))
            {
                throw new Domain.Exceptions.DomainException("Không thể kiểm kê cho ngày trong tương lai.");
            }

            var productIds = input.Lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await _asyncExecuter.ToListAsync(
                (await _products.GetQueryableAsync()).Where(p => productIds.Contains(p.Id)));
            var byId = products.ToDictionary(p => p.Id, p => p.Name);

            foreach (var line in input.Lines)
            {
                if (line.RemainingQty < 0)
                {
                    throw new Domain.Exceptions.DomainException("Số lượng còn lại không được âm.");
                }

                if (!byId.ContainsKey(line.ProductId))
                {
                    throw new Domain.Exceptions.DomainException("Sản phẩm không tồn tại.");
                }
            }

            // Replace-all for the date: delete existing lines, then insert.
            var existing = await _asyncExecuter.ToListAsync(
                (await _inventories.GetQueryableAsync()).Where(x => x.Date == date));
            foreach (var e in existing)
            {
                await _inventories.DeleteAsync(e);
            }

            var byUser = CurrentUser?.UserName;
            foreach (var line in input.Lines)
            {
                await _inventories.InsertAsync(new DailyInventory
                {
                    Date = date,
                    ProductId = line.ProductId,
                    ProductName = byId[line.ProductId],
                    RemainingQty = line.RemainingQty,
                    Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
                    CreatedBy = byUser,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        public virtual async Task<List<DailyInventoryDayDto>> GetHistoryAsync(int days = 30)
        {
            if (days < 1) days = 1;
            if (days > 90) days = 90;

            var all = await _asyncExecuter.ToListAsync(
                (await _inventories.GetQueryableAsync())
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
