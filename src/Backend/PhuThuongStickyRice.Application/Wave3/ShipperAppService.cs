using PhuThuongStickyRice.Application.Wave3.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Wave3
{
    /// <summary>Item 82: delivery shipper management (Admin).</summary>
    public interface IShipperAppService : IApplicationService
    {
        Task<List<ShipperDto>> GetListAsync(bool activeOnly = false);

        Task<ShipperDto> CreateAsync(CreateShipperDto input);

        Task<ShipperDto> UpdateAsync(Guid id, UpdateShipperDto input);

        Task DeleteAsync(Guid id);
    }

    [Authorize(Roles = "Admin")]
    public class ShipperAppService : ApplicationService, IShipperAppService
    {
        private readonly IRepository<Shipper, Guid> _shippers;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public ShipperAppService(
            IRepository<Shipper, Guid> shippers,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _shippers = shippers;
            _asyncExecuter = asyncExecuter;
        }

        public async Task<List<ShipperDto>> GetListAsync(bool activeOnly = false)
        {
            var queryable = await _shippers.GetQueryableAsync();
            System.Linq.IQueryable<Shipper> query = queryable.OrderBy(x => x.Name);
            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await _asyncExecuter.ToListAsync(query);
            return items.Select(Map).ToList();
        }

        public async Task<ShipperDto> CreateAsync(CreateShipperDto input)
        {
            var shipper = new Shipper
            {
                Name = input.Name.Trim(),
                Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _shippers.InsertAsync(shipper, autoSave: true);
            return Map(shipper);
        }

        public async Task<ShipperDto> UpdateAsync(Guid id, UpdateShipperDto input)
        {
            var shipper = await _shippers.GetAsync(id);
            shipper.Name = input.Name.Trim();
            shipper.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            shipper.IsActive = input.IsActive;
            await _shippers.UpdateAsync(shipper, autoSave: true);
            return Map(shipper);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _shippers.DeleteAsync(id, autoSave: true);
        }

        private static ShipperDto Map(Shipper s) => new ShipperDto
        {
            Id = s.Id,
            Name = s.Name,
            Phone = s.Phone,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}
