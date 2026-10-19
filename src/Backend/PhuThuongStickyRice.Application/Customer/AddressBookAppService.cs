
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Customer
{
        /// <summary>
        /// Sổ địa chỉ giao hàng đã lưu theo SĐT (dùng ở checkout).
        /// Khách không cần đăng nhập: SĐT là khóa định danh. Địa chỉ trùng
        /// nhãn (label) thì ghi đè thay vì tạo trùng — checkout hiện dropdown
        /// "Nhà"/"Công ty"/... cho khách chọn nhanh thay vì nhập lại.
        /// </summary>
        public interface IAddressBookAppService : IApplicationService
        {
            /// <summary>Public: saved addresses for a phone (tối đa 50, mới nhất trước).</summary>
            /// <param name="phone">SĐT khách hàng (được chuẩn hóa về dạng chữ số).</param>
            /// <returns>Danh sách địa chỉ đã lưu, mới nhất trước.</returns>
            Task<List<AddressBookDto>> GetByPhoneAsync(string phone);

            /// <summary>Public: upsert an address (dedupe by label).</summary>
            /// <param name="dto">SĐT, nhãn và nội dung địa chỉ.</param>
            /// <returns>Địa chỉ sau khi lưu (tạo mới hoặc ghi đè).</returns>
            Task<AddressBookDto> SaveAsync(SaveAddressDto dto);

            /// <summary>Public: delete one entry (phone must match).</summary>
            /// <param name="id">Id bản ghi địa chỉ cần xóa.</param>
            /// <param name="phone">SĐT chủ địa chỉ — phải khớp SĐT lưu trên bản ghi mới được xóa.</param>
            Task DeleteAsync(Guid id, string phone);
        }

        /// <summary>
        /// Sổ địa chỉ giao hàng đã lưu theo SĐT (public — dùng ở checkout).
        /// Không yêu cầu đăng nhập: khách vãng lai vẫn dùng được sổ địa chỉ
        /// của mình qua SĐT. Lưu tối đa 50 địa chỉ/SĐT, trùng nhãn thì ghi đè.
        /// </summary>
        public class AddressBookAppService : ApplicationService, IAddressBookAppService
        {
            private readonly IRepository<AddressBook, Guid> _addresses;
            private readonly IAsyncQueryableExecuter _asyncExecuter;
            private readonly IMemoryCache _memoryCache;

            /// <summary>Số địa chỉ đã lưu tối đa trả về cho 1 SĐT (endpoint public).</summary>
            private const int MaxAddressesPerPhone = 50;

            /// <summary>
            /// TTL cache cho <see cref="GetByPhoneAsync"/> (public, checkout gọi
            /// mỗi lần nhập SĐT — giống các public path khác: 5 phút).
            /// </summary>
            private static readonly TimeSpan PhoneCacheTtl = TimeSpan.FromMinutes(5);

            /// <summary>Tiền tố key cache cho <see cref="GetByPhoneAsync"/>.</summary>
            private const string PhoneCacheKeyPrefix = "addressbook:by-phone:";

            /// <summary>
            /// Khởi tạo service sổ địa chỉ.
            /// </summary>
            /// <param name="addresses">Repository địa chỉ đã lưu.</param>
            /// <param name="asyncExecuter">Thực thi truy vấn bất đồng bộ.</param>
            /// <param name="memoryCache">Cache in-memory (giống ShortLinkAppService).</param>
            public AddressBookAppService(
                IRepository<AddressBook, Guid> addresses,
                IAsyncQueryableExecuter asyncExecuter,
                IMemoryCache memoryCache)
            {
                _addresses = addresses;
                _asyncExecuter = asyncExecuter;
                _memoryCache = memoryCache;
            }

            /// <summary>
            /// Public: lấy địa chỉ đã lưu của 1 SĐT cho checkout (tối đa 50,
            /// mới nhất trước). SĐT không hợp lệ trả về danh sách rỗng thay vì
            /// lỗi — form checkout chỉ ẩn dropdown địa chỉ đã lưu.
            /// Kết quả được cache 5 phút theo SĐT chuẩn hóa vì checkout gọi
            /// mỗi lần nhập SĐT; <see cref="SaveAsync"/>/<see cref="DeleteAsync"/>
            /// là 2 writer duy nhất nên invalidate đúng key là đủ nhất quán
            /// (không dùng Invalidate() toàn cục để không bust cache nóng khác).
            /// </summary>
            /// <param name="phone">SĐT khách hàng (được chuẩn hóa về dạng chữ số).</param>
            /// <returns>Danh sách địa chỉ đã lưu, mới nhất trước.</returns>
            [AllowAnonymous]
            public virtual async Task<List<AddressBookDto>> GetByPhoneAsync(string phone)
            {
                var normalized = NormalizePhone(phone);
                if (normalized == null)
                {
                    return new List<AddressBookDto>();
                }

                var cacheKey = PhoneCacheKeyPrefix + normalized;
                if (_memoryCache.TryGetValue(cacheKey, out List<AddressBookDto> cached))
                {
                    // Trả bản sao: mỗi request trước đây đều nhận list mới,
                    // không để caller mutate chung list trong cache.
                    return new List<AddressBookDto>(cached);
                }

                var queryable = await _addresses.GetQueryableAsync();
                // perf-hunt 2026-10-19: Take(50) server-side — endpoint public
                // (checkout), trước đây materialize toàn bộ địa chỉ đã lưu của SĐT.
                var entities = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.Phone == normalized)
                        .OrderByDescending(x => x.CreatedDateTime)
                        .Take(MaxAddressesPerPhone));
                var dtos = entities.Select(MapToDto).ToList();
                _memoryCache.Set(cacheKey, dtos, PhoneCacheTtl);
                return new List<AddressBookDto>(dtos);
            }

            /// <summary>
            /// Public: lưu địa chỉ giao hàng vào sổ của SĐT. Cùng SĐT + nhãn
            /// (ví dụ "Nhà") mà lưu lại thì ghi đè địa chỉ cũ — vì 1 nhãn chỉ
            /// trỏ được 1 địa chỉ, tạo trùng sẽ làm dropdown checkout rối.
            /// </summary>
            /// <param name="dto">SĐT, nhãn và nội dung địa chỉ.</param>
            /// <returns>Địa chỉ sau khi lưu (tạo mới hoặc ghi đè).</returns>
            [AllowAnonymous]
            public virtual async Task<AddressBookDto> SaveAsync(SaveAddressDto dto)
            {
                var phone = NormalizePhone(dto.Phone);
                if (phone == null)
                {
                    throw new Domain.Exceptions.DomainException("Số điện thoại không hợp lệ.");
                }

                var label = dto.Label.Trim();
                var address = dto.Address.Trim();

                var queryable = await _addresses.GetQueryableAsync();
                var existing = await _asyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.Phone == phone && x.Label == label));

                if (existing != null)
                {
                    existing.Address = address;
                    await _addresses.UpdateAsync(existing, autoSave: true);
                    // perf-hunt 2026-10-19: gỡ cache ngay để checkout thấy
                    // địa chỉ vừa sửa mà không chờ hết TTL.
                    _memoryCache.Remove(PhoneCacheKeyPrefix + phone);
                    return MapToDto(existing);
                }

                var entity = new AddressBook
                {
                    Phone = phone,
                    Label = label,
                    Address = address
                };
                await _addresses.InsertAsync(entity, autoSave: true);
                // perf-hunt 2026-10-19: gỡ cache ngay để checkout thấy
                // địa chỉ vừa lưu mà không chờ hết TTL.
                _memoryCache.Remove(PhoneCacheKeyPrefix + phone);
                return MapToDto(entity);
            }

            /// <summary>
            /// Public: xóa 1 địa chỉ đã lưu. Endpoint public nên bắt buộc SĐT
            /// truyền vào phải khớp SĐT lưu trên bản ghi — nếu không bất kỳ ai
            /// biết Id cũng xóa được địa chỉ của khách khác.
            /// </summary>
            /// <param name="id">Id bản ghi địa chỉ cần xóa.</param>
            /// <param name="phone">SĐT chủ địa chỉ — phải khớp SĐT lưu trên bản ghi mới được xóa.</param>
            [AllowAnonymous]
            public virtual async Task DeleteAsync(Guid id, string phone)
            {
                var entity = await _addresses.GetAsync(id);
                var normalized = NormalizePhone(phone);
                if (normalized == null || entity.Phone != normalized)
                {
                    throw new Domain.Exceptions.DomainException("Không có quyền xóa địa chỉ này.");
                }

                await _addresses.DeleteAsync(entity, autoSave: true);
                // perf-hunt 2026-10-19: gỡ cache ngay để địa chỉ vừa xóa
                // biến mất khỏi dropdown checkout ngay lập tức.
                _memoryCache.Remove(PhoneCacheKeyPrefix + entity.Phone);
                _memoryCache.Remove(PhoneCacheKeyPrefix + normalized);
            }

            /// <summary>
            /// Chuẩn hóa SĐT về dạng chữ số thuần — dùng bản duy nhất ở
            /// <see cref="VietnamTime.NormalizePhone"/> (xóa bản copy-paste
            /// trước đây nằm ở đây): lọc bỏ dấu cách/dấu chấm rồi yêu cầu
            /// ít nhất 9 chữ số, số ngắn hơn coi như không hợp lệ để chặn rác.
            /// </summary>
            /// <param name="phone">SĐT nhập vào.</param>
            /// <returns>Chuỗi chữ số, hoặc null nếu không hợp lệ.</returns>
            private static string NormalizePhone(string phone) => VietnamTime.NormalizePhone(phone);

            /// <summary>Map entity địa chỉ đã lưu → DTO trả về API.</summary>
            private static AddressBookDto MapToDto(AddressBook entity)
            {
                return new AddressBookDto
                {
                    Id = entity.Id,
                    Phone = entity.Phone,
                    Label = entity.Label,
                    Address = entity.Address
                };
            }
        }

}
