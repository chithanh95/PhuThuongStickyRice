
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Customer;

namespace PhuThuongStickyRice.Application.Customer
{
        public interface IAddressBookAppService : IApplicationService
        {
            /// <summary>Public: saved addresses for a phone (tối đa 50, mới nhất trước).</summary>
            Task<List<AddressBookDto>> GetByPhoneAsync(string phone);

            /// <summary>Public: upsert an address (dedupe by label).</summary>
            Task<AddressBookDto> SaveAsync(SaveAddressDto dto);

            /// <summary>Public: delete one entry (phone must match).</summary>
            Task DeleteAsync(Guid id, string phone);
        }

        public class AddressBookAppService : ApplicationService, IAddressBookAppService
        {
            private readonly IRepository<AddressBook, Guid> _addresses;
            private readonly IAsyncQueryableExecuter _asyncExecuter;

            /// <summary>Số địa chỉ đã lưu tối đa trả về cho 1 SĐT (endpoint public).</summary>
            private const int MaxAddressesPerPhone = 50;

            public AddressBookAppService(
                IRepository<AddressBook, Guid> addresses,
                IAsyncQueryableExecuter asyncExecuter)
            {
                _addresses = addresses;
                _asyncExecuter = asyncExecuter;
            }

            [AllowAnonymous]
            public virtual async Task<List<AddressBookDto>> GetByPhoneAsync(string phone)
            {
                var normalized = NormalizePhone(phone);
                if (normalized == null)
                {
                    return new List<AddressBookDto>();
                }

                var queryable = await _addresses.GetQueryableAsync();
                // perf-hunt 2026-10-19: Take(50) server-side — endpoint public
                // (checkout), trước đây materialize toàn bộ địa chỉ đã lưu của SĐT.
                var entities = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.Phone == normalized)
                        .OrderByDescending(x => x.CreatedDateTime)
                        .Take(MaxAddressesPerPhone));
                return entities.Select(MapToDto).ToList();
            }

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
                    return MapToDto(existing);
                }

                var entity = new AddressBook
                {
                    Phone = phone,
                    Label = label,
                    Address = address
                };
                await _addresses.InsertAsync(entity, autoSave: true);
                return MapToDto(entity);
            }

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
            }

            private static string NormalizePhone(string phone)
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return null;
                }

                var digits = new string(phone.Where(char.IsDigit).ToArray());
                return digits.Length >= 9 ? digits : null;
            }

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
