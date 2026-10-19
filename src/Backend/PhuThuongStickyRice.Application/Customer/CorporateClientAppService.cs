
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Customer
{
    public interface ICorporateClientAppService : IApplicationService
    {
        Task<List<CorporateClientDto>> GetListAsync();
        Task<CorporateClientDto> GetAsync(Guid id);
        Task<CorporateClientDto> CreateAsync(CreateUpdateCorporateClientDto input);
        Task<CorporateClientDto> UpdateAsync(Guid id, CreateUpdateCorporateClientDto input);
        Task DeleteAsync(Guid id);

        /// <summary>Active clients whose standing order falls on today's weekday.</summary>
        Task<List<CorporateClientDto>> GetDueTodayAsync();
    }

    /// <summary>Item 122: corporate weekly-order customers.</summary>
    [Authorize(Roles = "Admin")]
    public class CorporateClientAppService : ApplicationService, ICorporateClientAppService
    {
        private readonly IRepository<CorporateClient, Guid> _clients;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CorporateClientAppService(
            IRepository<CorporateClient, Guid> clients,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _clients = clients;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<List<CorporateClientDto>> GetListAsync()
        {
            var queryable = await _clients.GetQueryableAsync();
            // perf-hunt 2026-10-19 (run 14:15): chặn số dòng ở server (Take 200)
            // như các admin list khác — bảng khách hàng doanh nghiệp tăng
            // theo thời gian, không nên materialize toàn bộ mỗi lần mở trang.
            var list = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.CompanyName).Take(200));
            return list.Select(MapToDto).ToList();
        }

        public virtual async Task<CorporateClientDto> GetAsync(Guid id)
        {
            return MapToDto(await _clients.GetAsync(id));
        }

        public virtual async Task<CorporateClientDto> CreateAsync(CreateUpdateCorporateClientDto input)
        {
            var entity = new CorporateClient
            {
                CompanyName = input.CompanyName.Trim(),
                ContactName = string.IsNullOrWhiteSpace(input.ContactName) ? null : input.ContactName.Trim(),
                Phone = VietnamTime.NormalizePhone(input.Phone) ?? input.Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim(),
                Weekday = input.Weekday,
                ItemsJson = string.IsNullOrWhiteSpace(input.ItemsJson) ? null : input.ItemsJson,
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _clients.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        public virtual async Task<CorporateClientDto> UpdateAsync(Guid id, CreateUpdateCorporateClientDto input)
        {
            var entity = await _clients.GetAsync(id);
            entity.CompanyName = input.CompanyName.Trim();
            entity.ContactName = string.IsNullOrWhiteSpace(input.ContactName) ? null : input.ContactName.Trim();
            entity.Phone = VietnamTime.NormalizePhone(input.Phone) ?? input.Phone.Trim();
            entity.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
            entity.Weekday = input.Weekday;
            entity.ItemsJson = string.IsNullOrWhiteSpace(input.ItemsJson) ? null : input.ItemsJson;
            entity.IsActive = input.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _clients.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _clients.DeleteAsync(id, autoSave: true);
        }

        public virtual async Task<List<CorporateClientDto>> GetDueTodayAsync()
        {
            var weekday = (int)VietnamTime.Today().DayOfWeek;
            var queryable = await _clients.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive && x.Weekday == weekday)
                    .OrderBy(x => x.CompanyName));
            return list.Select(MapToDto).ToList();
        }

        private static CorporateClientDto MapToDto(CorporateClient x)
        {
            return new CorporateClientDto
            {
                Id = x.Id,
                CompanyName = x.CompanyName,
                ContactName = x.ContactName,
                Phone = x.Phone,
                Address = x.Address,
                Weekday = x.Weekday,
                WeekdayName = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.DayNames[x.Weekday],
                ItemsJson = x.ItemsJson,
                IsActive = x.IsActive
            };
        }
    }
}
