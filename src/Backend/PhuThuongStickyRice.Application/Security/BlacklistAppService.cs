
// CS8632: file dùng annotation nullable (?); bật ngữ cảnh annotations để
// compiler hiểu đúng ý nghĩa, không bật phân tích luồng (dự án chưa bật nullable toàn cục).
#nullable enable annotations

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
        // [codequality-practices 2026-10-20] string?: xem ghi chú ở implementation.
        Task<bool> IsBlacklistedAsync(string? phone);
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Item 88: quản lý danh sách SĐT bị chặn (blacklist).
    /// Checkout từ chối các SĐT trong danh sách này — dùng để chặn khách
    /// quỵt đơn/bom hàng mà vẫn cho phép mua lại sau khi Admin gỡ chặn.
    /// </summary>
    public class BlacklistAppService : ApplicationService, IBlacklistAppService
    {
        /// <summary>Repository bảng BlacklistedPhone (SĐT bị chặn).</summary>
        private readonly IRepository<BlacklistedPhone, Guid> _blacklist;

        /// <summary>Thực thi query async trên IQueryable (ABP).</summary>
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        // [codequality-smell] Magic number: số dòng danh sách chặn tối đa
        // trả về 1 lần — đặt hằng số có tên để thay đổi 1 chỗ.
        private const int MaxListRows = 500;

        /// <summary>Khởi tạo service quản lý danh sách chặn SĐT.</summary>
        /// <param name="blacklist">Repository bảng BlacklistedPhone.</param>
        /// <param name="asyncExecuter">Executor chạy query LINQ async.</param>
        public BlacklistAppService(
            IRepository<BlacklistedPhone, Guid> blacklist,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _blacklist = blacklist;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Admin: lấy danh sách SĐT bị chặn, mới nhất trước.
        /// </summary>
        /// <returns>Danh sách SĐT bị chặn (tối đa 500 bản ghi mới nhất).</returns>
        public virtual async Task<List<BlacklistedPhoneDto>> GetListAsync()
        {
            var queryable = await _blacklist.GetQueryableAsync();
            // perf-hunt 2026-10-19: Take(MaxListRows) server-side — danh sách chặn
            // tăng dần theo thời gian, không bao giờ materialize toàn bộ bảng.
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedAt).Take(MaxListRows));
            return items.Select(x => new BlacklistedPhoneDto
            {
                Id = x.Id,
                Phone = x.Phone,
                Reason = x.Reason,
                CreatedAt = x.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Admin: thêm một SĐT vào danh sách chặn.
        /// Kiểm tra trùng theo SĐT đã chuẩn hóa để cùng một số không bị
        /// chặn 2 lần dù nhập khác định dạng (vd: 090... và +84 90...).
        /// </summary>
        /// <param name="input">SĐT cần chặn và lý do.</param>
        /// <returns>SĐT vừa được thêm vào danh sách chặn.</returns>
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

        /// <summary>
        /// Admin: xóa một SĐT khỏi danh sách chặn (khách được đặt hàng lại).
        /// </summary>
        /// <param name="id">Id bản ghi blacklist.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _blacklist.DeleteAsync(id, autoSave: true);
        }

        /// <summary>
        /// Kiểm tra một SĐT có nằm trong danh sách chặn không.
        /// Cho phép ẩn danh vì checkout public cần gọi trước khi tạo đơn.
        /// </summary>
        /// <param name="phone">SĐT cần kiểm tra (mọi định dạng).</param>
        /// <returns>true nếu SĐT bị chặn.</returns>
        // AllowAnonymous: luồng checkout public (khách chưa đăng nhập) cần
        // gọi được hàm này để từ chối SĐT bị chặn ngay trước khi tạo đơn.
        // [codequality-practices 2026-10-20] string?: SĐT từ input user có thể
        // null — impl đã null-safe (NormalizePhone → rỗng → trả false).
        [AllowAnonymous]
        public virtual async Task<bool> IsBlacklistedAsync(string? phone)
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

        /// <summary>
        /// Chuẩn hóa SĐT về dạng chỉ gồm chữ số, đầu số 0 (vd: "+84 901 234 567" → "0901234567").
        /// Nhờ đó cùng một số dù nhập khác định dạng vẫn so sánh trùng được.
        /// </summary>
        /// <param name="phone">SĐT ở mọi định dạng.</param>
        /// <returns>Chuỗi chữ số chuẩn hóa; rỗng nếu không có số nào.</returns>
        // [codequality-practices 2026-10-20] string?: input từ user/API có thể
        // null — IsNullOrWhiteSpace ở dưới đã xử lý, trả string.Empty.
        // codequality-team 2026-10-20 Agent2-CleanCode: ủy thác cho
        // <see cref="PhoneNumber.NormalizeToLocalDigits"/> — trước đây là
        // bản copy-paste trùng với RewardRuleAppService.
        public static string NormalizePhone(string? phone) =>
            PhoneNumber.NormalizeToLocalDigits(phone);
    }
}
