
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Staff;

namespace PhuThuongStickyRice.Application.Staff
{
    public interface IShiftCloseAppService : IApplicationService
    {
        /// <summary>Expected COD for a business day (VN date).</summary>
        Task<decimal> GetExpectedCodAsync(DateTime date);

        Task<ShiftCloseDto> GetByDateAsync(DateTime date);

        Task<ShiftCloseDto> CloseAsync(CloseShiftInput input);

        Task<List<ShiftCloseDto>> GetListAsync(int take = 30);
    }

    /// <summary>Item 129: COD end-of-shift reconciliation.</summary>
    [Authorize(Roles = "Admin,Cashier")]
    public class ShiftCloseAppService : ApplicationService, IShiftCloseAppService
    {
        private readonly IRepository<ShiftClose, Guid> _closes;
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public ShiftCloseAppService(
            IRepository<ShiftClose, Guid> closes,
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _closes = closes;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<decimal> GetExpectedCodAsync(DateTime date)
        {
            var day = date.Date;
            var startUtc = day.AddHours(-7); // VN 00:00 → UTC
            var endUtc = startUtc.AddDays(1);

            var queryable = await _orders.GetQueryableAsync();
            var orders = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc
                    && x.Status == OrderStatus.Completed
                    && x.PaymentMethod == PaymentMethod.Cod));

            return orders.Sum(x => x.TotalAmount.Amount);
        }

        public virtual async Task<ShiftCloseDto> GetByDateAsync(DateTime date)
        {
            var day = date.Date;
            var queryable = await _closes.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Date == day));
            return entity == null ? null : MapToDto(entity);
        }

        public virtual async Task<ShiftCloseDto> CloseAsync(CloseShiftInput input)
        {
            var day = input.Date.Date;
            var expected = await GetExpectedCodAsync(day);

            var queryable = await _closes.GetQueryableAsync();
            var existing = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Date == day));

            if (existing == null)
            {
                existing = new ShiftClose
                {
                    Date = day,
                    CreatedBy = CurrentUser?.Email ?? CurrentUser?.UserName ?? "system",
                    CreatedAt = DateTime.UtcNow
                };
                await _closes.InsertAsync(existing);
            }

            existing.ExpectedCod = expected;
            existing.ActualCod = Math.Max(0, input.ActualCod);
            existing.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            await _closes.UpdateAsync(existing, autoSave: true);

            return MapToDto(existing);
        }

        public virtual async Task<List<ShiftCloseDto>> GetListAsync(int take = 30)
        {
            var queryable = await _closes.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.Date).Take(Math.Max(1, Math.Min(100, take))));
            return list.Select(MapToDto).ToList();
        }

        private static ShiftCloseDto MapToDto(ShiftClose x)
        {
            return new ShiftCloseDto
            {
                Id = x.Id,
                Date = x.Date,
                ExpectedCod = x.ExpectedCod,
                ActualCod = x.ActualCod,
                Discrepancy = x.ActualCod - x.ExpectedCod,
                Note = x.Note,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt
            };
        }
    }
}
