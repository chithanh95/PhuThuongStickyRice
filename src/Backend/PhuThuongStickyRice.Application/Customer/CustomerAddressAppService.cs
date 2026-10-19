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
    /// <summary>
    /// Địa chỉ giao hàng đã lưu của khách (sổ địa chỉ ở trang tài khoản):
    /// mỗi user chỉ thấy/sửa/xóa địa chỉ của chính mình, mỗi user tối đa 1
    /// địa chỉ mặc định. SĐT người nhận phải đúng đầu số di động VN.
    /// </summary>
    public interface ICustomerAddressAppService : IApplicationService
    {
        /// <summary>Đã đăng nhập: lấy địa chỉ đã lưu của tôi (mặc định lên trước).</summary>
        Task<List<CustomerAddressDto>> GetMyAsync();

        /// <summary>Đã đăng nhập: thêm địa chỉ mới vào sổ địa chỉ của tôi.</summary>
        Task<CustomerAddressDto> CreateAsync(CreateCustomerAddressDto dto);

        /// <summary>Đã đăng nhập: sửa 1 địa chỉ của tôi.</summary>
        Task<CustomerAddressDto> UpdateAsync(Guid id, UpdateCustomerAddressDto dto);

        /// <summary>Đã đăng nhập: xóa 1 địa chỉ của tôi.</summary>
        Task DeleteAsync(Guid id);

        /// <summary>Đã đăng nhập: đặt 1 địa chỉ của tôi làm mặc định (địa chỉ mặc định cũ tự bỏ).</summary>
        Task SetDefaultAsync(Guid id);
    }

    /// <summary>
    /// Quản lý sổ địa chỉ giao hàng của khách đã đăng nhập: lấy/thêm/sửa/xóa/
    /// đặt mặc định. Mọi thao tác đều kiểm tra địa chỉ thuộc về user đang
    /// đăng nhập (GetOwnedAsync) — không ai chạm được địa chỉ của người khác.
    /// </summary>
    public partial class CustomerAddressAppService : ApplicationService, ICustomerAddressAppService
    {
        // Best practice .NET 7+/C# 13: [GeneratedRegex] biên dịch regex ngay
        // lúc build (không tốn Regex.Compile/cache động ở runtime) — cùng
        // convention với MessengerChatService/MoMoPaymentService trong cùng
        // codebase. Ngữ nghĩa kiểm tra SĐT giống hệt bản Regex.IsMatch cũ.
        // SĐT di động VN 10 số: bắt đầu bằng 0, đầu số 03/05/07/08/09.
        [GeneratedRegex(@"^(0)(3|5|7|8|9)\d{8}$")]
        private static partial Regex ReceiverPhoneRegex();

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

        /// <summary>Lấy sổ địa chỉ của tôi: mặc định lên trước, rồi mới nhất.
        /// Giới hạn 100 bản ghi ở SQL để payload/ram luôn có trần (thực tế
        /// 1 người hiếm khi quá vài chục địa chỉ).</summary>
        public virtual async Task<List<CustomerAddressDto>> GetMyAsync()
        {
            var userId = RequireUserId();
            var queryable = await _addresses.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.IsDefault)
                    .ThenByDescending(x => x.CreatedAt)
                    .Take(100));
            return entities.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Thêm địa chỉ mới: validate SĐT, chuẩn hóa khoảng trắng. Nếu đánh dấu
        /// mặc định thì bỏ cờ mặc định của các địa chỉ cũ trước (ClearDefaultAsync).
        /// </summary>
        /// <param name="dto">Thông tin địa chỉ mới.</param>
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

        /// <summary>
        /// Sửa địa chỉ của tôi: chỉ khi địa chỉ thuộc về user đang đăng nhập.
        /// Đổi sang mặc định → bỏ cờ các địa chỉ cũ; bỏ cờ mặc định thì địa
        /// chỉ này không còn là mặc định (không ép phải có mặc định).
        /// </summary>
        /// <param name="id">Id địa chỉ cần sửa.</param>
        /// <param name="dto">Thông tin mới.</param>
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

        /// <summary>Xóa địa chỉ của tôi (chỉ khi địa chỉ thuộc về user đang đăng nhập).</summary>
        /// <param name="id">Id địa chỉ cần xóa.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            var userId = RequireUserId();
            var entity = await GetOwnedAsync(id, userId);
            await _addresses.DeleteAsync(entity);
        }

        /// <summary>Đặt 1 địa chỉ làm mặc định: bỏ cờ mặc định của các địa chỉ cũ rồi gán cho địa chỉ này.</summary>
        /// <param name="id">Id địa chỉ cần đặt mặc định.</param>
        public virtual async Task SetDefaultAsync(Guid id)
        {
            var userId = RequireUserId();
            var entity = await GetOwnedAsync(id, userId);
            await ClearDefaultAsync(userId);
            entity.IsDefault = true;
            await _addresses.UpdateAsync(entity);
        }

        /// <summary>
        /// Lấy địa chỉ theo id và kiểm tra sở hữu: địa chỉ không thuộc user
        /// đang đăng nhập → 403 (chống dò/xem địa chỉ của người khác).
        /// </summary>
        private async Task<CustomerAddress> GetOwnedAsync(Guid id, Guid userId)
        {
            var entity = await _addresses.GetAsync(id);
            if (entity.UserId != userId)
            {
                throw new AbpAuthorizationException("Bạn không có quyền thao tác địa chỉ này.");
            }

            return entity;
        }

        /// <summary>Bỏ cờ mặc định của mọi địa chỉ khác của user — đảm bảo tối đa 1 địa chỉ mặc định.</summary>
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
            ArgumentNullException.ThrowIfNull(dto);

            if (!ReceiverPhoneRegex().IsMatch((dto.ReceiverPhone ?? string.Empty).Trim()))
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
