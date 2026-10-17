using PhuThuongStickyRice.Application.Zalo;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Customer
{
    public interface ICustomerProfileAppService : IApplicationService
    {
        /// <summary>Authenticated customer: my taste preferences + birthday.</summary>
        Task<CustomerProfileDto> GetMyAsync();

        /// <summary>Authenticated customer: update my taste preferences / birthday.</summary>
        Task<CustomerProfileDto> UpdateAsync(UpdateCustomerProfileDto dto);

        /// <summary>Admin: registered customers with a birthday in the given month.</summary>
        Task<List<BirthdayMemberDto>> GetBirthdaysAsync(int month);

        /// <summary>Admin: grant this year's birthday gift (one-time discount code).</summary>
        Task<BirthdayGiftResultDto> GrantBirthdayGiftAsync(Guid userId, GrantBirthdayGiftDto dto);

        /// <summary>Admin/Cashier: remind an inactive customer (Zalo OA stub).</summary>
        Task<CustomerReminderDto> CreateReminderAsync(CreateReminderDto dto);

        /// <summary>Admin/Cashier: reminder history for a phone number.</summary>
        Task<List<CustomerReminderDto>> GetRemindersAsync(string phone);
    }

    public class CustomerProfileAppService : ApplicationService, ICustomerProfileAppService
    {
        public const string CustomerRoleName = "Customer";
        private const string GiftCodePrefix = "SN";

        private readonly IRepository<User, Guid> _users;
        private readonly IRepository<UserRole, Guid> _userRoles;
        private readonly IRepository<Role, Guid> _roles;
        private readonly IRepository<DiscountCode, Guid> _discounts;
        private readonly IRepository<CustomerReminder, Guid> _reminders;
        private readonly IZaloNotificationService _zalo;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IStaffActivityLogger _activityLogger;

        public CustomerProfileAppService(
            IRepository<User, Guid> users,
            IRepository<UserRole, Guid> userRoles,
            IRepository<Role, Guid> roles,
            IRepository<DiscountCode, Guid> discounts,
            IRepository<CustomerReminder, Guid> reminders,
            IZaloNotificationService zalo,
            IAsyncQueryableExecuter asyncExecuter,
            IStaffActivityLogger activityLogger)
        {
            _users = users;
            _userRoles = userRoles;
            _roles = roles;
            _discounts = discounts;
            _reminders = reminders;
            _zalo = zalo;
            _asyncExecuter = asyncExecuter;
            _activityLogger = activityLogger;
        }

        public virtual async Task<CustomerProfileDto> GetMyAsync()
        {
            var user = await GetCurrentUserAsync();
            var dto = new CustomerProfileDto();
            ParseTaste(user.TastePreferences, dto);
            dto.DateOfBirth = user.DateOfBirth;
            dto.CanChangeDateOfBirth = CanChangeDob(user, out var hint);
            dto.DateOfBirthChangeHint = hint;

            var thisYear = DateTime.UtcNow.Year;
            if (user.BirthdayGiftYear == thisYear && !string.IsNullOrWhiteSpace(user.BirthdayGiftCode))
            {
                dto.BirthdayGiftCode = user.BirthdayGiftCode;
                var code = await FindDiscountCodeAsync(user.BirthdayGiftCode);
                if (code != null)
                {
                    dto.BirthdayGiftAmount = code.DiscountAmount;
                    dto.BirthdayGiftExpiresAt = code.ExpiresAt;
                }
            }

            return dto;
        }

        public virtual async Task<CustomerProfileDto> UpdateAsync(UpdateCustomerProfileDto dto)
        {
            var user = await GetCurrentUserAsync();
            dto ??= new UpdateCustomerProfileDto();

            var tags = (dto.TasteTags ?? new List<string>())
                .Select(t => (t ?? string.Empty).Trim())
                .Where(t => t.Length > 0)
                .Distinct()
                .Take(10)
                .ToList();
            if (tags.Any(t => t.Length > 50))
            {
                throw new UserFriendlyException("Mỗi khẩu vị tối đa 50 ký tự.");
            }

            var note = (dto.TasteNote ?? string.Empty).Trim();
            if (note.Length > 500)
            {
                throw new UserFriendlyException("Ghi chú khẩu vị tối đa 500 ký tự.");
            }

            user.TastePreferences = JsonSerializer.Serialize(new TastePrefs { Tags = tags, Note = note });

            if (dto.DateOfBirth.HasValue)
            {
                var dob = dto.DateOfBirth.Value.Date;
                if (dob >= DateTime.UtcNow.Date)
                {
                    throw new UserFriendlyException("Ngày sinh phải là ngày trong quá khứ.");
                }

                if (dob < DateTime.UtcNow.Date.AddYears(-120))
                {
                    throw new UserFriendlyException("Ngày sinh không hợp lệ.");
                }

                var dobChanged = user.DateOfBirth != dob;
                if (dobChanged)
                {
                    if (!CanChangeDob(user, out _))
                    {
                        throw new UserFriendlyException("Ngày sinh chỉ được thay đổi 1 lần mỗi năm. Vui lòng liên hệ quán nếu nhập nhầm.");
                    }

                    user.DateOfBirth = dob;
                    user.DateOfBirthUpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _users.UpdateAsync(user);
            return await GetMyAsync();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<BirthdayMemberDto>> GetBirthdaysAsync(int month)
        {
            if (month < 1 || month > 12)
            {
                throw new UserFriendlyException("Tháng không hợp lệ.");
            }

            var thisYear = DateTime.UtcNow.Year;
            var role = await FindCustomerRoleAsync();

            var userQuery = await _users.GetQueryableAsync();
            var roleQuery = await _userRoles.GetQueryableAsync();

            var query = from u in userQuery
                        join ur in roleQuery on u.Id equals ur.UserId
                        where ur.RoleId == role.Id
                              && u.DateOfBirth.HasValue
                              && u.DateOfBirth.Value.Month == month
                        orderby u.DateOfBirth.Value.Day
                        select u;

            var users = await _asyncExecuter.ToListAsync(query);
            return users.Select(u => new BirthdayMemberDto
            {
                UserId = u.Id,
                Phone = u.PhoneNumber,
                UserName = u.UserName,
                DateOfBirth = u.DateOfBirth,
                HasGiftThisYear = u.BirthdayGiftYear == thisYear,
                GiftCode = u.BirthdayGiftYear == thisYear ? u.BirthdayGiftCode : null
            }).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<BirthdayGiftResultDto> GrantBirthdayGiftAsync(Guid userId, GrantBirthdayGiftDto dto)
        {
            var user = await _users.GetAsync(userId);
            var phone = (user.PhoneNumber ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                throw new UserFriendlyException("Tài khoản này chưa có SĐT hợp lệ, không thể tặng quà.");
            }

            var thisYear = DateTime.UtcNow.Year;
            if (user.BirthdayGiftYear == thisYear)
            {
                throw new UserFriendlyException(
                    $"Đã tặng quà sinh nhật năm {thisYear} rồi (mã {user.BirthdayGiftCode}).");
            }

            var amount = dto?.DiscountAmount ?? 25000;
            if (amount < 5000 || amount > 1000000)
            {
                throw new UserFriendlyException("Mệnh giá quà từ 5.000₫ đến 1.000.000₫.");
            }

            var code = await GenerateUniqueCodeAsync();
            var expiresAt = DateTime.UtcNow.AddDays(30);

            await _discounts.InsertAsync(new DiscountCode
            {
                Code = code,
                DiscountAmount = amount,
                DiscountPercent = null,
                MinOrderAmount = 0,
                ExpiresAt = expiresAt,
                MaxUses = 1,
                UsedCount = 0,
                IsActive = true,
                VoucherType = VoucherType.Discount,
                OwnerPhone = phone
            }, autoSave: true);

            user.BirthdayGiftYear = thisYear;
            user.BirthdayGiftCode = code;
            await _users.UpdateAsync(user);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.BirthdayGift,
                $"Tặng quà sinh nhật {amount:N0}₫ (mã {code}) cho SĐT {phone}.");

            return new BirthdayGiftResultDto
            {
                Code = code,
                DiscountAmount = amount,
                ExpiresAt = expiresAt
            };
        }

        [Authorize(Roles = "Admin,Cashier")]
        public virtual async Task<CustomerReminderDto> CreateReminderAsync(CreateReminderDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var phone = (dto.Phone ?? string.Empty).Trim();
            if (!IsValidVnPhone(phone))
            {
                throw new UserFriendlyException("SĐT không đúng định dạng (VD: 0901234567).");
            }

            var message = (dto.Message ?? string.Empty).Trim();
            if (message.Length == 0 || message.Length > CustomerReminder.MessageMaxLength)
            {
                throw new UserFriendlyException($"Tin nhắn từ 1 đến {CustomerReminder.MessageMaxLength} ký tự.");
            }

            var entity = new CustomerReminder
            {
                CustomerPhone = phone,
                CustomerName = (dto.CustomerName ?? string.Empty).Trim(),
                Message = message,
                SentVia = "ZaloOA",
                CreatedBy = CurrentUser?.UserName
            };
            await _reminders.InsertAsync(entity, autoSave: true);

            // Zalo OA is a stub until credentials are configured — it logs
            // instead of sending. The DB row above is the honest history.
            await _zalo.SendTextMessageAsync(phone, message);

            return MapReminder(entity);
        }

        [Authorize(Roles = "Admin,Cashier")]
        public virtual async Task<List<CustomerReminderDto>> GetRemindersAsync(string phone)
        {
            phone = (phone ?? string.Empty).Trim();
            var queryable = await _reminders.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.CustomerPhone == phone)
                    .OrderByDescending(x => x.CreatedAt));
            return entities.Select(MapReminder).ToList();
        }

        private async Task<User> GetCurrentUserAsync()
        {
            if (CurrentUser?.Id == null)
            {
                throw new AbpAuthorizationException("Bạn cần đăng nhập.");
            }

            return await _users.GetAsync(CurrentUser.Id.Value);
        }

        private static bool CanChangeDob(User user, out string hint)
        {
            hint = null;
            if (user.DateOfBirthUpdatedAt == null)
            {
                return true;
            }

            var nextAllowed = user.DateOfBirthUpdatedAt.Value.AddYears(1);
            if (DateTimeOffset.UtcNow >= nextAllowed)
            {
                return true;
            }

            hint = $"Đổi được lại sau ngày {nextAllowed:dd/MM/yyyy}.";
            return false;
        }

        private async Task<Role> FindCustomerRoleAsync()
        {
            var queryable = await _roles.GetQueryableAsync();
            var role = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Name == CustomerRoleName));
            if (role == null)
            {
                throw new UserFriendlyException("Chưa có vai trò khách hàng trong hệ thống.");
            }

            return role;
        }

        private async Task<DiscountCode> FindDiscountCodeAsync(string code)
        {
            var queryable = await _discounts.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == code));
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var chars = new char[6];
                var bytes = RandomNumberGenerator.GetBytes(6);
                for (var i = 0; i < 6; i++)
                {
                    chars[i] = alphabet[bytes[i] % alphabet.Length];
                }

                var code = GiftCodePrefix + new string(chars);
                if (await FindDiscountCodeAsync(code) == null)
                {
                    return code;
                }
            }

            throw new UserFriendlyException("Không tạo được mã quà, vui lòng thử lại.");
        }

        private static void ParseTaste(string json, CustomerProfileDto dto)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var prefs = JsonSerializer.Deserialize<TastePrefs>(json);
                    dto.TasteTags = prefs?.Tags ?? new List<string>();
                    dto.TasteNote = prefs?.Note;
                    return;
                }
            }
            catch (JsonException)
            {
                // Fall through to empty.
            }

            dto.TasteTags = new List<string>();
        }

        private static CustomerReminderDto MapReminder(CustomerReminder entity)
        {
            return new CustomerReminderDto
            {
                Id = entity.Id,
                CustomerPhone = entity.CustomerPhone,
                CustomerName = entity.CustomerName,
                Message = entity.Message,
                SentVia = entity.SentVia,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            };
        }

        private static bool IsValidVnPhone(string phone)
        {
            return Regex.IsMatch(phone ?? string.Empty, @"^(0)(3|5|7|8|9)\d{8}$");
        }

        private class TastePrefs
        {
            public List<string> Tags { get; set; } = new List<string>();

            public string Note { get; set; }
        }
    }
}
