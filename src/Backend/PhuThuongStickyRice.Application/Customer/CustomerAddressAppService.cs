using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Customer
{
    public interface ICustomerAddressAppService : IApplicationService
    {
        /// <summary>Authenticated: list my saved addresses.</summary>
        Task<List<CustomerAddressDto>> GetMyAsync();

        /// <summary>Authenticated: add a saved address.</summary>
        Task<CustomerAddressDto> CreateAsync(CreateCustomerAddressDto dto);

        /// <summary>Authenticated: update one of my addresses.</summary>
        Task<CustomerAddressDto> UpdateAsync(Guid id, UpdateCustomerAddressDto dto);

        /// <summary>Authenticated: delete one of my addresses.</summary>
        Task DeleteAsync(Guid id);

        /// <summary>Authenticated: mark one of my addresses as default.</summary>
        Task SetDefaultAsync(Guid id);
    }

    public class CustomerAddressAppService : ApplicationService, ICustomerAddressAppService
    {
        private readonly IRepository<CustomerAddress, Guid> _addresses;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CustomerAddressAppService(
            IRepository<CustomerAddress, Guid> addresses,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _addresses = addresses;
            _asyncExecuter = asyncExecuter;
        }

        private Guid RequireUserId()
        {
            if (CurrentUser?.Id == null)
            {
                throw new AbpAuthorizationException("Bạn cần đăng nhập.");
            }

            return CurrentUser.Id.Value;
        }

        public virtual async Task<List<CustomerAddressDto>> GetMyAsync()
        {
            var userId = RequireUserId();
            var queryable = await _addresses.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.IsDefault)
                    .ThenByDescending(x => x.CreatedAt));
            return entities.Select(MapToDto).ToList();
        }

        public virtual async Task<CustomerAddressDto> CreateAsync(CreateCustomerAddressDto dto)
        {
            var userId = RequireUserId();
            Validate(dto);

            var entity = new CustomerAddress
            {
                UserId = userId,
                Label = dto.Label?.Trim(),
                ReceiverName = dto.ReceiverName.Trim(),
                ReceiverPhone = dto.ReceiverPhone.Trim(),
                AddressDetail = dto.AddressDetail.Trim(),
                Note = dto.Note?.Trim(),
                IsDefault = dto.IsDefault
            };

            if (entity.IsDefault)
            {
                await ClearDefaultAsync(userId);
            }

            await _addresses.InsertAsync(entity);
            return MapToDto(entity);
        }

        public virtual async Task<CustomerAddressDto> UpdateAsync(Guid id, UpdateCustomerAddressDto dto)
        {
            var userId = RequireUserId();
            Validate(dto);

            var entity = await GetOwnedAsync(id, userId);
            entity.Label = dto.Label?.Trim();
            entity.ReceiverName = dto.ReceiverName.Trim();
            entity.ReceiverPhone = dto.ReceiverPhone.Trim();
            entity.AddressDetail = dto.AddressDetail.Trim();
            entity.Note = dto.Note?.Trim();

            if (dto.IsDefault && !entity.IsDefault)
            {
                await ClearDefaultAsync(userId);
                entity.IsDefault = true;
            }
            else if (!dto.IsDefault)
            {
                entity.IsDefault = false;
            }

            await _addresses.UpdateAsync(entity);
            return MapToDto(entity);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var userId = RequireUserId();
            var entity = await GetOwnedAsync(id, userId);
            await _addresses.DeleteAsync(entity);
        }

        public virtual async Task SetDefaultAsync(Guid id)
        {
            var userId = RequireUserId();
            var entity = await GetOwnedAsync(id, userId);
            await ClearDefaultAsync(userId);
            entity.IsDefault = true;
            await _addresses.UpdateAsync(entity);
        }

        private async Task<CustomerAddress> GetOwnedAsync(Guid id, Guid userId)
        {
            var entity = await _addresses.GetAsync(id);
            if (entity.UserId != userId)
            {
                throw new AbpAuthorizationException("Bạn không có quyền thao tác địa chỉ này.");
            }

            return entity;
        }

        private async Task ClearDefaultAsync(Guid userId)
        {
            var queryable = await _addresses.GetQueryableAsync();
            var defaults = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.UserId == userId && x.IsDefault));
            foreach (var defaultAddress in defaults)
            {
                defaultAddress.IsDefault = false;
                await _addresses.UpdateAsync(defaultAddress);
            }
        }

        private static void Validate(CreateCustomerAddressDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (!Regex.IsMatch((dto.ReceiverPhone ?? string.Empty).Trim(), @"^(0)(3|5|7|8|9)\d{8}$"))
            {
                throw new UserFriendlyException("Số điện thoại người nhận không đúng định dạng (VD: 0901234567).");
            }
        }

        private static CustomerAddressDto MapToDto(CustomerAddress entity)
        {
            return new CustomerAddressDto
            {
                Id = entity.Id,
                Label = entity.Label,
                ReceiverName = entity.ReceiverName,
                ReceiverPhone = entity.ReceiverPhone,
                AddressDetail = entity.AddressDetail,
                Note = entity.Note,
                IsDefault = entity.IsDefault
            };
        }
    }
}
