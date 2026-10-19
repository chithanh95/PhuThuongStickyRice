using PhuThuongStickyRice.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
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

        /// <summary>
        /// Quản lý khách VIP dựa trên tag "VIP" của hệ thống tag khách hàng.
        /// SĐT luôn được chuẩn hóa trước khi so sánh để "090..." và "+84..."
        /// được nhận diện là cùng một khách.
        /// </summary>
    public class VipPriorityAppService : ApplicationService, IVipPriorityAppService
    {
        public const string VipTagName = "VIP";

        private const string VipPhonesCacheKey = "vip:phones";
        private static readonly TimeSpan VipPhonesCacheTtl = TimeSpan.FromMinutes(5);

        private readonly ICustomerTagAppService _tags;
        private readonly IRepository<CustomerTag, Guid> _tagRepo;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IMemoryCache _memoryCache;

        public VipPriorityAppService(
            ICustomerTagAppService tags,
            IRepository<CustomerTag, Guid> tagRepo,
            IAsyncQueryableExecuter asyncExecuter,
            IMemoryCache memoryCache)
        {
            _tags = tags;
            _tagRepo = tagRepo;
            _asyncExecuter = asyncExecuter;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Kiểm tra một SĐT có đang được gắn tag VIP không.
        /// </summary>
        /// <param name="phone">Số điện thoại cần kiểm tra.</param>
        /// <returns>True nếu là khách VIP.</returns>
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

        /// <summary>
        /// Kiểm tra VIP cho nhiều SĐT trong một lần gọi — gom thành một query duy nhất
        /// thay vì gọi lẻ từng SĐT để tránh N+1 query khi xử lý danh sách đơn.
        /// </summary>
        /// <param name="phones">Danh sách SĐT cần kiểm tra.</param>
        /// <returns>Dictionary SĐT → có phải VIP không (SĐT lạ/không hợp lệ trả false).</returns>
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

        /// <summary>
        /// Lấy toàn bộ SĐT đang gắn tag VIP (dạng tập hợp, đã loại trùng) để ưu tiên
        /// xử lý đơn / áp chính sách ưu đãi cho khách VIP.
        /// perf-hunt 2026-10-19: cache 5 phút — admin order list gọi mỗi request
        /// khi bật filter VIP. Mark/UnmarkVip bust cache; tag CRUD thủ công
        /// (qua CustomerTagAppService) được TTL 5 phút bao phủ.
        /// </summary>
        /// <returns>Tập hợp SĐT VIP.</returns>
        public virtual async Task<HashSet<string>> GetVipPhonesAsync()
        {
            if (_memoryCache.TryGetValue(VipPhonesCacheKey, out HashSet<string> cached))
            {
                return cached;
            }

            var queryable = await _tagRepo.GetQueryableAsync();
            var phones = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.TagName == VipTagName)
                    .Select(x => x.CustomerPhone));
            var result = new HashSet<string>(phones.Distinct());
            _memoryCache.Set(VipPhonesCacheKey, result, VipPhonesCacheTtl);
            return result;
        }

        /// <summary>
        /// Lấy danh sách khách VIP đầy đủ thông tin tag, mới gắn nhất lên trước (admin).
        /// </summary>
        /// <returns>Danh sách tag VIP.</returns>
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

        /// <summary>
        /// Gắn tag VIP cho khách. Idempotent — khách đã là VIP thì trả về tag hiện có,
        /// không tạo tag trùng.
        /// </summary>
        /// <param name="phone">Số điện thoại khách.</param>
        /// <returns>Tag VIP của khách.</returns>
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

            var added = await _tags.AddTagAsync(new CreateCustomerTagDto
            {
                CustomerPhone = phone,
                TagName = VipTagName,
                Color = CustomerTagPresets.ColorFor(VipTagName),
            });
            _memoryCache.Remove(VipPhonesCacheKey);
            return added;
        }

        /// <summary>
        /// Gỡ tag VIP khỏi khách; SĐT trống hoặc khách không phải VIP thì bỏ qua,
        /// không báo lỗi.
        /// </summary>
        /// <param name="phone">Số điện thoại khách.</param>
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
                _memoryCache.Remove(VipPhonesCacheKey);
            }
        }

        private static string NormalizePhone(string phone) =>
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
