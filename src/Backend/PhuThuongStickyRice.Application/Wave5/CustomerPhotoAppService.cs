using PhuThuongStickyRice.Application.Wave5.DTOs;
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

namespace PhuThuongStickyRice.Application.Wave5
{
    public interface ICustomerPhotoAppService : IApplicationService
    {
        /// <summary>Public: approved photos for the gallery.</summary>
        Task<List<CustomerPhotoDto>> GetApprovedAsync();

        /// <summary>Public: registers a photo record after the controller saved the file.</summary>
        Task<CustomerPhotoDto> RegisterAsync(string imageUrl, CreateCustomerPhotoDto input);

        /// <summary>Admin: all photos (optionally pending only).</summary>
        Task<List<CustomerPhotoDto>> GetListAsync(bool pendingOnly);

        /// <summary>Admin: approve → appears in the public gallery.</summary>
        Task ApproveAsync(Guid id);

        /// <summary>Admin: reject → record deleted (controller removes the file).</summary>
        Task<CustomerPhotoDto> RejectAsync(Guid id);
    }

    /// <summary>Item 123: UGC gallery photos.</summary>
    public class CustomerPhotoAppService : ApplicationService, ICustomerPhotoAppService
    {
        private readonly IRepository<CustomerPhoto, Guid> _photos;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CustomerPhotoAppService(
            IRepository<CustomerPhoto, Guid> photos,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _photos = photos;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public virtual async Task<List<CustomerPhotoDto>> GetApprovedAsync()
        {
            var queryable = await _photos.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsApproved)
                    .OrderByDescending(x => x.CreatedAt));
            return list.Select(MapToDto).ToList();
        }

        [AllowAnonymous]
        public virtual async Task<CustomerPhotoDto> RegisterAsync(string imageUrl, CreateCustomerPhotoDto input)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new DomainException("Image is required.");
            }

            var phone = VietnamTime.NormalizePhone(input?.Phone);
            if (!string.IsNullOrEmpty(input?.Phone) && string.IsNullOrEmpty(phone))
            {
                throw new DomainException("Số điện thoại không hợp lệ.");
            }

            var photo = new CustomerPhoto
            {
                ImageUrl = imageUrl,
                Caption = string.IsNullOrWhiteSpace(input.Caption) ? null : input.Caption.Trim(),
                Phone = phone,
                Name = string.IsNullOrWhiteSpace(input.Name) ? null : input.Name.Trim(),
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };
            await _photos.InsertAsync(photo, autoSave: true);
            return MapToDto(photo);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<CustomerPhotoDto>> GetListAsync(bool pendingOnly)
        {
            var queryable = await _photos.GetQueryableAsync();
            IQueryable<CustomerPhoto> query = queryable.OrderByDescending(x => x.CreatedAt);
            if (pendingOnly)
            {
                query = query.Where(x => !x.IsApproved);
            }

            var list = await _asyncExecuter.ToListAsync(query);
            return list.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task ApproveAsync(Guid id)
        {
            var photo = await _photos.GetAsync(id);
            photo.IsApproved = true;
            await _photos.UpdateAsync(photo, autoSave: true);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<CustomerPhotoDto> RejectAsync(Guid id)
        {
            var photo = await _photos.GetAsync(id);
            await _photos.DeleteAsync(photo, autoSave: true);
            return MapToDto(photo);
        }

        private static CustomerPhotoDto MapToDto(CustomerPhoto x)
        {
            return new CustomerPhotoDto
            {
                Id = x.Id,
                ImageUrl = x.ImageUrl,
                Caption = x.Caption,
                Name = x.Name,
                IsApproved = x.IsApproved,
                CreatedAt = x.CreatedAt
            };
        }
    }
}
