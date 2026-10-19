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
    /// <summary>
    /// Hợp đồng hồ sơ khách hàng: khách xem/cập nhật khẩu vị + ngày sinh của
    /// chính mình; admin xem danh sách sinh nhật theo tháng, tặng quà sinh
    /// nhật, và chăm sóc khách (nhắc Zalo OA).
    /// </summary>
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

    /// <summary>
    /// Hồ sơ khách hàng. Hai nghiệp vụ chính:
    /// <list type="bullet">
    /// <item>Khách tự quản lý khẩu vị + ngày sinh (ngày sinh chỉ đổi 1 lần/năm
    /// để chống gian lận quà sinh nhật).</item>
    /// <item>Admin tặng quà sinh nhật: mỗi năm mỗi khách 1 mã giảm giá dùng 1 lần,
    /// hiệu lực 30 ngày (ghi log hoạt động nhân viên, item 156).</item>
    /// </list>
    /// </summary>
    public class CustomerProfileAppService : ApplicationService, ICustomerProfileAppService
    {
        public const string CustomerRoleName = "Customer";
        private const string GiftCodePrefix = "SN";
        /// <summary>Độ dài phần ngẫu nhiên của mã quà sinh nhật (đứng sau tiền tố SN).</summary>
        private const int GiftCodeRandomPartLength = 6;
        /// <summary>Số lần thử tạo mã quà duy nhất tối đa trước khi báo lỗi.</summary>
        private const int GiftCodeMaxAttempts = 20;
        /// <summary>Mệnh giá quà mặc định (VNĐ) khi admin không nhập.</summary>
        private const int DefaultBirthdayGiftAmount = 25000;
        /// <summary>Mệnh giá quà sinh nhật tối thiểu/tối đa cho phép (VNĐ).</summary>
        private const int MinBirthdayGiftAmount = 5000;
        private const int MaxBirthdayGiftAmount = 1000000;
        /// <summary>Mã quà sinh nhật có hiệu lực 30 ngày kể từ ngày tặng.</summary>
        private const int BirthdayGiftValidityDays = 30;
        /// <summary>SĐT Việt Nam: bắt đầu bằng 0, đầu số 3/5/7/8/9, tổng 10 chữ số.</summary>
        private const string VnPhonePattern = @"^(0)(3|5|7|8|9)\d{8}$";

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

        /// <summary>
        /// Khách xem hồ sơ của chính mình: khẩu vị, ngày sinh (+ có được đổi
        /// không), và mã quà sinh nhật năm nay nếu đã được tặng.
        /// </summary>
        /// <returns>Hồ sơ khách hàng dưới dạng DTO.</returns>
        public virtual async Task<CustomerProfileDto> GetMyAsync()
        {
            var user = await GetCurrentUserAsync();
            var dto = new CustomerProfileDto();
            ParseTaste(user.TastePreferences, dto);
            dto.DateOfBirth = user.DateOfBirth;
            dto.CanChangeDateOfBirth = CanChangeDob(user, out var hint);
            dto.DateOfBirthChangeHint = hint;

            var thisYear = DateTime.UtcNow.Year;
            // Chỉ hiện mã quà khi năm lưu trên user trùng năm hiện tại —
            // quà năm cũ hết hiệu lực thì không hiện lại cho khách.
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

        /// <summary>
        /// Khách cập nhật khẩu vị (tối đa 10 tag, mỗi tag 50 ký tự, ghi chú 500 ký tự)
        /// và/hoặc ngày sinh. Ngày sinh chỉ được đổi 1 lần mỗi năm kể từ lần đổi
        /// gần nhất (chống gian lận nhận quà sinh nhật); quá giới hạn thì báo lỗi
        /// kèm ngày được đổi lại.
        /// </summary>
        /// <param name="dto">Danh sách tag khẩu vị, ghi chú khẩu vị, ngày sinh mới.</param>
        /// <returns>Hồ sơ sau khi cập nhật.</returns>
        /// <exception cref="UserFriendlyException">Ném khi ngày sinh không hợp lệ hoặc chưa được phép đổi.</exception>
        public virtual async Task<CustomerProfileDto> UpdateAsync(UpdateCustomerProfileDto dto)
        {
            var user = await GetCurrentUserAsync();
            dto ??= new UpdateCustomerProfileDto();

            user.TastePreferences = SerializeTastePrefs(dto);

            if (dto.DateOfBirth.HasValue)
            {
                var dob = ValidateDateOfBirth(dto.DateOfBirth.Value.Date);

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

        private const int MaxTasteTags = 10;
        private const int MaxTagLength = 50;
        private const int MaxTasteNoteLength = 500;
        private const int MaxDobAgeYears = 120;

        /// <summary>Validates and serializes the customer's taste preferences.</summary>
        private static string SerializeTastePrefs(UpdateCustomerProfileDto dto)
        {
            var tags = (dto.TasteTags ?? new List<string>())
                .Select(t => (t ?? string.Empty).Trim())
                .Where(t => t.Length > 0)
                .Distinct()
                .Take(MaxTasteTags)
                .ToList();
            if (tags.Any(t => t.Length > MaxTagLength))
            {
                throw new UserFriendlyException($"Mỗi khẩu vị tối đa {MaxTagLength} ký tự.");
            }

            var note = (dto.TasteNote ?? string.Empty).Trim();
            if (note.Length > MaxTasteNoteLength)
            {
                throw new UserFriendlyException($"Ghi chú khẩu vị tối đa {MaxTasteNoteLength} ký tự.");
            }

            return JsonSerializer.Serialize(new TastePrefs { Tags = tags, Note = note });
        }

        /// <summary>Validates a date of birth (past date, not older than 120 years).</summary>
        private static DateTime ValidateDateOfBirth(DateTime dob)
        {
            if (dob >= DateTime.UtcNow.Date)
            {
                throw new UserFriendlyException("Ngày sinh phải là ngày trong quá khứ.");
            }

            if (dob < DateTime.UtcNow.Date.AddYears(-MaxDobAgeYears))
            {
                throw new UserFriendlyException("Ngày sinh không hợp lệ.");
            }

            return dob;
        }

        /// <summary>
        /// Admin xem danh sách khách (role Customer) có sinh nhật trong tháng
        /// cho trước, sắp xếp theo ngày sinh tăng dần. Mỗi dòng kèm trạng thái
        /// đã tặng quà năm nay chưa.
        /// </summary>
        /// <param name="month">Tháng (1–12).</param>
        /// <returns>Danh sách khách sinh nhật trong tháng.</returns>
        /// <exception cref="UserFriendlyException">Ném khi tháng ngoài 1–12.</exception>
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

        /// <summary>
        /// Admin tặng quà sinh nhật cho 1 khách: sinh mã giảm giá dùng 1 lần,
        /// tiền tố "SN", mệnh giá 5.000₫–1.000.000₫ (mặc định 25.000₫), hiệu lực
        /// 30 ngày, gắn đúng SĐT khách. Mỗi khách chỉ nhận 1 lần mỗi năm —
        /// tặng trùng năm thì báo lỗi kèm mã đã tặng. Ghi log hoạt động nhân viên.
        /// </summary>
        /// <param name="userId">Id tài khoản khách (phải có SĐT hợp lệ).</param>
        /// <param name="dto">Mệnh giá quà (để trống thì dùng mặc định 25.000₫).</param>
        /// <returns>Mã quà, mệnh giá và ngày hết hạn.</returns>
        /// <exception cref="UserFriendlyException">Ném khi SĐT không hợp lệ, đã tặng năm nay, hoặc mệnh giá ngoài giới hạn.</exception>
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

            var amount = dto?.DiscountAmount ?? DefaultBirthdayGiftAmount;
            if (amount < MinBirthdayGiftAmount || amount > MaxBirthdayGiftAmount)
            {
                throw new UserFriendlyException("Mệnh giá quà từ 5.000₫ đến 1.000.000₫.");
            }

            var code = await GenerateUniqueCodeAsync();
            var expiresAt = DateTime.UtcNow.AddDays(BirthdayGiftValidityDays);

            await InsertBirthdayDiscountCodeAsync(code, amount, expiresAt, phone);

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

        /// <summary>
        /// Admin/Cashier tạo nhắc chăm sóc khách (vd gọi lại khách lâu không mua).
        /// Lưu lịch sử vào DB và gửi tin nhắn qua Zalo OA (hiện là stub, chỉ ghi
        /// log khi chưa cấu hình credentials — dòng DB là lịch sử chính xác).
        /// </summary>
        /// <param name="dto">SĐT, tên khách, nội dung tin nhắn.</param>
        /// <returns>Nhắc đã tạo.</returns>
        /// <exception cref="UserFriendlyException">Ném khi SĐT sai định dạng hoặc tin nhắn rỗng/quá dài.</exception>
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

        /// <summary>
        /// Admin/Cashier xem lịch sử nhắc chăm sóc của 1 SĐT, mới nhất lên trước.
        /// </summary>
        /// <param name="phone">Số điện thoại cần xem lịch sử.</param>
        /// <returns>Danh sách nhắc đã tạo cho SĐT.</returns>
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

        /// <summary>
        /// Ngày sinh chỉ được đổi 1 lần mỗi năm kể từ lần đổi gần nhất —
        /// chống việc khách đổi ngày sinh liên tục để nhận nhiều quà sinh nhật.
        /// </summary>
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

        /// <summary>
        /// Tách từ GrantBirthdayGiftAsync: tạo hàng mã giảm giá dùng 1 lần
        /// cho quà sinh nhật. Tách ra để GrantBirthdayGiftAsync gọn dưới
        /// 50 dòng, dễ đọc hơn.
        /// </summary>
        private async Task InsertBirthdayDiscountCodeAsync(string code, decimal amount, DateTime expiresAt, string phone)
        {
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
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            // Bảng chữ cái cố ý bỏ I/O (dễ nhầm 1), số 0/1 (dễ nhầm O/l) để
            // mã quà đọc qua điện thoại cho khách không bị nhầm.
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            for (var attempt = 0; attempt < GiftCodeMaxAttempts; attempt++)
            {
                var chars = new char[GiftCodeRandomPartLength];
                var bytes = RandomNumberGenerator.GetBytes(GiftCodeRandomPartLength);
                for (var i = 0; i < GiftCodeRandomPartLength; i++)
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
            return Regex.IsMatch(phone ?? string.Empty, VnPhonePattern);
        }

        private class TastePrefs
        {
            public List<string> Tags { get; set; } = new List<string>();

            public string Note { get; set; }
        }
    }
}
