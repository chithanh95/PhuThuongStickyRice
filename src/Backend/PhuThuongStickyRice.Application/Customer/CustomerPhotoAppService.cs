
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
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Hợp đồng quản lý ảnh do khách hàng gửi (item 123 — gallery UGC):
    /// khách gửi ảnh sau khi controller đã lưu file; admin duyệt/từ chối;
    /// ảnh đã duyệt hiện ở gallery công khai.
    /// </summary>
    public interface ICustomerPhotoAppService : IApplicationService
    {
        /// <summary>Public: ảnh đã duyệt cho gallery (cache 5 phút).</summary>
        Task<List<CustomerPhotoDto>> GetApprovedAsync();

        /// <summary>
        /// Public: đăng ký 1 bản ghi ảnh sau khi controller đã lưu file.
        /// Ảnh mới ở trạng thái chờ duyệt (không hiện ở gallery công khai).
        /// </summary>
        /// <param name="imageUrl">URL file ảnh controller đã lưu.</param>
        /// <param name="input">Chú thích, tên, SĐT người gửi (SĐT không bắt buộc).</param>
        /// <returns>Bản ghi ảnh vừa tạo.</returns>
        Task<CustomerPhotoDto> RegisterAsync(string imageUrl, CreateCustomerPhotoDto input);

        /// <summary>Admin: toàn bộ ảnh; <c>pendingOnly</c> = true thì chỉ ảnh chờ duyệt.</summary>
        /// <param name="pendingOnly">Chỉ lấy ảnh chưa duyệt.</param>
        /// <returns>Danh sách ảnh, mới nhất lên trước, tối đa 500.</returns>
        Task<List<CustomerPhotoDto>> GetListAsync(bool pendingOnly);

        /// <summary>Admin: duyệt ảnh → ảnh xuất hiện ở gallery công khai.</summary>
        /// <param name="id">Id ảnh cần duyệt.</param>
        Task ApproveAsync(Guid id);

        /// <summary>Admin: từ chối ảnh → xóa bản ghi (controller xóa file ảnh).</summary>
        /// <param name="id">Id ảnh bị từ chối.</param>
        /// <returns>Bản ghi ảnh vừa bị từ chối.</returns>
        Task<CustomerPhotoDto> RejectAsync(Guid id);
    }

    /// <summary>
    /// Item 123: gallery ảnh do khách hàng gửi (UGC). Khách gửi ảnh mới ở trạng
    /// thái chờ duyệt; admin duyệt thì ảnh hiện ở gallery công khai, từ chối
    /// thì xóa bản ghi. Gallery công khai được cache 5 phút vì trang gallery
    /// gọi API này mỗi lượt xem.
    /// </summary>
    public class CustomerPhotoAppService : ApplicationService, ICustomerPhotoAppService
    {
        private readonly IRepository<CustomerPhoto, Guid> _photos;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Cache cho gallery công khai (danh sách không giới hạn, bị query mỗi lượt xem trang).</summary>
        private readonly Products.IProductCatalogCache _catalogCache;

        /// <summary>
        /// Số ảnh UGC đã duyệt tối đa nạp vào cache public — ảnh UGC tăng vô
        /// hạn theo thời gian, phải có giới hạn để entry cache / payload trả về
        /// không phình.
        /// </summary>
        private const int MaxApprovedPhotosCached = 200;

        /// <summary>
        /// Số ảnh chờ duyệt tối đa admin thấy mỗi lần mở trang — chặn để danh
        /// sách admin không materialize toàn bộ bảng mỗi lần mở.
        /// </summary>
        private const int MaxPendingPhotosListed = 500;

        /// <summary>
        /// Khởi tạo service gallery ảnh khách gửi với repository ảnh, bộ thực
        /// thi truy vấn bất đồng bộ và cache danh sách ảnh đã duyệt.
        /// </summary>
        /// <param name="photos">Repository bảng ảnh UGC.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn bất đồng bộ.</param>
        /// <param name="catalogCache">Cache gallery công khai (khóa customerphotos:approved-list).</param>
        public CustomerPhotoAppService(
            IRepository<CustomerPhoto, Guid> photos,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _photos = photos;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Public: ảnh đã duyệt cho gallery, mới nhất lên trước. Cache 5 phút;
        /// admin duyệt/từ chối ảnh đều invalidate để gallery thấy thay đổi ngay.
        /// </summary>
        /// <returns>Danh sách ảnh đã duyệt, tối đa 200 ảnh mới nhất.</returns>
        [AllowAnonymous]
        public virtual async Task<List<CustomerPhotoDto>> GetApprovedAsync()
        {
            // Trang gallery gọi API này mỗi lượt xem nên cache 5 phút; mỗi lần
            // admin duyệt/từ chối ảnh bên dưới đều invalidate cache.
            return await _catalogCache.GetOrAddAsync("customerphotos:approved-list", async () =>
            {
                var queryable = await _photos.GetQueryableAsync();
                // Ảnh UGC tăng vô hạn theo thời gian; chỉ cache N ảnh mới nhất
                // để entry cache / payload trả về luôn có giới hạn.
                var list = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.IsApproved)
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(MaxApprovedPhotosCached));
                return list.Select(MapToDto).ToList();
            });
        }

        /// <summary>
        /// Public: đăng ký bản ghi ảnh sau khi controller đã lưu file.
        /// Ảnh mới ở trạng thái chờ duyệt. SĐT (nếu có) phải chuẩn hóa được,
        /// nếu không ném lỗi để khách nhập lại đúng.
        /// </summary>
        /// <param name="imageUrl">URL file ảnh controller đã lưu.</param>
        /// <param name="input">Chú thích, tên, SĐT người gửi.</param>
        /// <returns>Bản ghi ảnh vừa tạo.</returns>
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
                throw new DomainException(PhoneNumber.InvalidPhoneMessage);
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

        /// <summary>
        /// Admin: toàn bộ ảnh (mới nhất lên trước, tối đa 500 — ảnh UGC tăng vô
        /// hạn, và hàng chờ duyệt vốn đã sắp xếp mới nhất lên trước).
        /// </summary>
        /// <param name="pendingOnly">True: chỉ lấy ảnh chưa duyệt.</param>
        /// <returns>Danh sách ảnh theo bộ lọc.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<CustomerPhotoDto>> GetListAsync(bool pendingOnly)
        {
            var queryable = await _photos.GetQueryableAsync();
            IQueryable<CustomerPhoto> query = queryable.OrderByDescending(x => x.CreatedAt);
            if (pendingOnly)
            {
                query = query.Where(x => !x.IsApproved);
            }

            // Ảnh UGC tăng vô hạn; chặn ở N ảnh mới nhất để danh sách admin
            // không materialize toàn bộ bảng mỗi lần mở trang.
            var list = await _asyncExecuter.ToListAsync(query.Take(MaxPendingPhotosListed));
            return list.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: duyệt ảnh — ảnh xuất hiện ở gallery công khai. Invalidate cache
        /// gallery để khách thấy ảnh mới ngay.
        /// </summary>
        /// <param name="id">Id ảnh cần duyệt.</param>
        [Authorize(Roles = "Admin")]
        public virtual async Task ApproveAsync(Guid id)
        {
            var photo = await _photos.GetAsync(id);
            photo.IsApproved = true;
            await _photos.UpdateAsync(photo, autoSave: true);
            // Xóa cache gallery công khai để ảnh vừa duyệt hiện ra ngay.
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Admin: từ chối ảnh — xóa bản ghi (controller xóa file ảnh).
        /// Invalidate cache gallery công khai.
        /// </summary>
        /// <param name="id">Id ảnh bị từ chối.</param>
        /// <returns>Bản ghi ảnh vừa bị từ chối.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<CustomerPhotoDto> RejectAsync(Guid id)
        {
            var photo = await _photos.GetAsync(id);
            await _photos.DeleteAsync(photo, autoSave: true);
            // Xóa cache gallery công khai để ảnh bị từ chối biến mất ngay.
            _catalogCache.Invalidate();
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
