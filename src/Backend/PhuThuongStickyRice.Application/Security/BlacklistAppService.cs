
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Security;

namespace PhuThuongStickyRice.Application.Security
{
    /// <summary>Item 88: phone blacklist (Admin CRUD). Checkout rejects these phones.</summary>
    public interface IBlacklistAppService : IApplicationService
    {
        Task<List<BlacklistedPhoneDto>> GetListAsync();

        Task<BlacklistedPhoneDto> CreateAsync(CreateBlacklistedPhoneDto input);

        Task DeleteAsync(Guid id);

        /// <summary>True when the phone is blacklisted (public checkout path uses this).</summary>
        Task<bool> IsBlacklistedAsync(string phone);
    }

    [Authorize(Roles = "Admin")]
    public class BlacklistAppService : ApplicationService, IBlacklistAppService
    {
        private readonly IRepository<BlacklistedPhone, Guid> _blacklist;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public BlacklistAppService(
            IRepository<BlacklistedPhone, Guid> blacklist,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _blacklist = blacklist;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<List<BlacklistedPhoneDto>> GetListAsync()
        {
            var queryable = await _blacklist.GetQueryableAsync();
            // perf-hunt 2026-10-19: Take(500) server-side — danh sách chặn
            // tăng dần theo thời gian, không bao giờ materialize toàn bộ bảng.
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedAt).Take(500));
            return items.Select(x => new BlacklistedPhoneDto
            {
                Id = x.Id,
                Phone = x.Phone,
                Reason = x.Reason,
                CreatedAt = x.CreatedAt
            }).ToList();
        }

        public virtual async Task<BlacklistedPhoneDto> CreateAsync(CreateBlacklistedPhoneDto input)
        {
            var normalized = NormalizePhone(input.Phone);
            var queryable = await _blacklist.GetQueryableAsync();
            var exists = await _asyncExecuter.AnyAsync(
                queryable.Where(x => x.NormalizedPhone == normalized));
            if (exists)
            {
                throw new Domain.Exceptions.DomainException("Số điện thoại này đã có trong danh sách chặn.");
            }

            var entry = new BlacklistedPhone
            {
                Phone = input.Phone.Trim(),
                NormalizedPhone = normalized,
                Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _blacklist.InsertAsync(entry, autoSave: true);
            return new BlacklistedPhoneDto
            {
                Id = entry.Id,
                Phone = entry.Phone,
                Reason = entry.Reason,
                CreatedAt = entry.CreatedAt
            };
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _blacklist.DeleteAsync(id, autoSave: true);
        }

        [AllowAnonymous]
        public virtual async Task<bool> IsBlacklistedAsync(string phone)
        {
            var normalized = NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            var queryable = await _blacklist.GetQueryableAsync();
            return await _asyncExecuter.AnyAsync(
                queryable.Where(x => x.NormalizedPhone == normalized));
        }

        /// <summary>Digits-only normalization so "+84 901 234 567" matches "0901234567".</summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            // Convert leading country code 84 -> 0 for local matching.
            if (digits.StartsWith("84") && digits.Length == 11)
            {
                digits = "0" + digits.Substring(2);
            }

            return digits;
        }
    }
}
