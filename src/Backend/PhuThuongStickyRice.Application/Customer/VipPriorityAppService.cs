using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Item 130 (đúng): VIP customer marking and priority.
    /// A VIP is a customer phone carrying the preset "VIP" <see cref="CustomerTag"/>
    /// (item 149) — no new table, no migration. This service gives VIP
    /// first-class, idempotent operations plus fast batch lookups so the
    /// order list, kitchen screen and priority sorting can treat VIPs
    /// as more than just another label.
    /// NOTE (perf-hunt 2026-10-19): lookups compare TagName with plain ==
    /// against <see cref="VipPriorityAppService.VipTagName"/> instead of
    /// ToLower() so SQL Server can seek the unique index
    /// {CustomerPhone, TagName}; matching stays case-insensitive under the
    /// database's default CI collation.
    /// </summary>
    public interface IVipPriorityAppService : IApplicationService
    {
        /// <summary>True when the phone carries the VIP tag.</summary>
        Task<bool> IsVipAsync(string phone);

        /// <summary>Batch VIP lookup (one query, no N+1).</summary>
        Task<Dictionary<string, bool>> IsVipByPhonesAsync(IEnumerable<string> phones);

        /// <summary>All phone numbers currently marked VIP.</summary>
        Task<HashSet<string>> GetVipPhonesAsync();

        /// <summary>VIP customers as tag DTOs (most recent first).</summary>
        Task<List<CustomerTagDto>> GetVipsAsync();

        /// <summary>
        /// Marks a phone VIP. Idempotent — marking twice is not an error.
        /// </summary>
        Task<CustomerTagDto> MarkVipAsync(string phone);

        /// <summary>
        /// Removes the VIP mark from a phone. Not an error when not VIP.
        /// </summary>
        Task UnmarkVipAsync(string phone);
    }

    public class VipPriorityAppService : ApplicationService, IVipPriorityAppService
    {
        public const string VipTagName = "VIP";

        private readonly ICustomerTagAppService _tags;
        private readonly IRepository<CustomerTag, Guid> _tagRepo;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public VipPriorityAppService(
            ICustomerTagAppService tags,
            IRepository<CustomerTag, Guid> tagRepo,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _tags = tags;
            _tagRepo = tagRepo;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<bool> IsVipAsync(string phone)
        {
            phone = NormalizePhone(phone);
            if (string.IsNullOrEmpty(phone))
            {
                return false;
            }

            var queryable = await _tagRepo.GetQueryableAsync();
            return await _asyncExecuter.AnyAsync(
                queryable.Where(x => x.CustomerPhone == phone
                    && x.TagName == VipTagName));
        }

        public virtual async Task<Dictionary<string, bool>> IsVipByPhonesAsync(IEnumerable<string> phones)
        {
            var list = (phones ?? Enumerable.Empty<string>())
                .Select(NormalizePhone)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();
            var result = list.ToDictionary(x => x, _ => false);
            if (list.Count == 0)
            {
                return result;
            }

            var queryable = await _tagRepo.GetQueryableAsync();
            var vipPhones = await _asyncExecuter.ToListAsync(
                queryable.Where(x => list.Contains(x.CustomerPhone)
                    && x.TagName == VipTagName)
                    .Select(x => x.CustomerPhone));
            foreach (var phone in vipPhones.Distinct())
            {
                result[phone] = true;
            }

            return result;
        }

        public virtual async Task<HashSet<string>> GetVipPhonesAsync()
        {
            var queryable = await _tagRepo.GetQueryableAsync();
            var phones = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.TagName == VipTagName)
                    .Select(x => x.CustomerPhone));
            return new HashSet<string>(phones.Distinct());
        }

        public virtual async Task<List<CustomerTagDto>> GetVipsAsync()
        {
            var queryable = await _tagRepo.GetQueryableAsync();
            var rows = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.TagName == VipTagName)
                    .OrderByDescending(x => x.CreatedAt));
            return rows.Select(x => new CustomerTagDto
            {
                Id = x.Id,
                CustomerPhone = x.CustomerPhone,
                TagName = x.TagName,
                Color = x.Color,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
            }).ToList();
        }

        public virtual async Task<CustomerTagDto> MarkVipAsync(string phone)
        {
            phone = NormalizePhone(phone);
            if (string.IsNullOrEmpty(phone))
            {
                throw new UserFriendlyException("Vui lòng nhập số điện thoại.");
            }

            var existing = await _tags.GetTagsAsync(phone);
            var vip = existing.FirstOrDefault(t =>
                string.Equals(t.TagName, VipTagName, StringComparison.OrdinalIgnoreCase));
            if (vip != null)
            {
                // Idempotent: already VIP, just return the tag.
                return vip;
            }

            return await _tags.AddTagAsync(new CreateCustomerTagDto
            {
                CustomerPhone = phone,
                TagName = VipTagName,
                Color = CustomerTagPresets.ColorFor(VipTagName),
            });
        }

        public virtual async Task UnmarkVipAsync(string phone)
        {
            phone = NormalizePhone(phone);
            if (string.IsNullOrEmpty(phone))
            {
                return;
            }

            var existing = await _tags.GetTagsAsync(phone);
            var vip = existing.FirstOrDefault(t =>
                string.Equals(t.TagName, VipTagName, StringComparison.OrdinalIgnoreCase));
            if (vip != null)
            {
                await _tags.DeleteTagAsync(vip.Id);
            }
        }

        private static string NormalizePhone(string phone) =>
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
