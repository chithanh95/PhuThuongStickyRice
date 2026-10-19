// codequality-team 2026-10-20 run-0245 Agent2-CleanCode
using PhuThuongStickyRice.Application.Catalog.DTOs;
using PhuThuongStickyRice.Application.Common;
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

namespace PhuThuongStickyRice.Application.Catalog
{
    /// <summary>
    /// Hợp đồng tiếp nhận báo giá số lượng lớn/sự kiện: khách gửi yêu cầu
    /// (không thanh toán), admin duyệt + đặt cọc; kèm nhắc cúng hàng tháng
    /// (nhắc rằm/mùng 1) theo SĐT đăng ký.
    /// </summary>
    public interface IQuoteRequestAppService : IApplicationService
    {
        /// <summary>Public: customer submits a bulk/event quote request. No payment.</summary>
        Task<QuoteRequestDto> CreateAsync(CreateQuoteRequestDto dto);

        Task<PagedResultDto<QuoteRequestDto>> GetListAsync(GetQuoteRequestsInput input);

        Task<QuoteRequestDto> UpdateStatusAsync(Guid id, UpdateQuoteRequestStatusDto dto);

        /// <summary>
        /// Item 161: admin đặt số tiền cọc + thông tin CK khi duyệt báo giá.
        /// </summary>
        Task<QuoteRequestDto> SetDepositAsync(Guid id, SetQuoteDepositDto dto);

        /// <summary>Public: subscribes a phone to monthly cung reminders.</summary>
        Task<CungReminderDto> CreateReminderAsync(CreateCungReminderDto dto);

        /// <summary>Admin: lists cung reminder subscriptions, newest first.</summary>
        Task<List<CungReminderDto>> GetRemindersAsync();

        /// <summary>Admin: toggles a reminder subscription on/off.</summary>
        Task<CungReminderDto> ToggleReminderAsync(Guid id);
    }

    /// <summary>
    /// Tiếp nhận yêu cầu báo giá đặt số lượng lớn/sự kiện từ trang
    /// "Đặt số lượng lớn". Không tạo đơn, không thanh toán — trạng thái ban
    /// đầu là <c>QuoteStatus.New</c> để admin xử lý thủ công.
    /// </summary>
    public partial class QuoteRequestAppService : ApplicationService, IQuoteRequestAppService
    {
        private readonly IRepository<QuoteRequest, Guid> _quotes;
        private readonly IRepository<CungReminder, Guid> _reminders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public QuoteRequestAppService(
            IRepository<QuoteRequest, Guid> quotes,
            IRepository<CungReminder, Guid> reminders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _quotes = quotes;
            _reminders = reminders;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Khách gửi yêu cầu báo giá (không cần đăng nhập). Kiểm tra định dạng
        /// SĐT Việt Nam; số lượng tối thiểu là 1.
        /// </summary>
        /// <param name="dto">Tên, SĐT, ngày/địa điểm sự kiện, ngân sách, số lượng, ghi chú.</param>
        /// <returns>Yêu cầu báo giá vừa tạo (trạng thái New).</returns>
        [AllowAnonymous]
        public virtual async Task<QuoteRequestDto> CreateAsync(CreateQuoteRequestDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            EnsureValidVnPhone(dto.CustomerPhone);

            var entity = new QuoteRequest
            {
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone.Trim(),
                EventDate = dto.EventDate?.Date,
                EventLocation = TrimToNull(dto.EventLocation),
                Budget = TrimToNull(dto.Budget),
                QuoteType = TrimToNull(dto.QuoteType),
                // Số lượng tối thiểu 1 — không cho đơn báo giá 0 suất.
                Quantity = Math.Max(1, dto.Quantity),
                Notes = TrimToNull(dto.Notes),
                Status = QuoteStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            await _quotes.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin xem danh sách yêu cầu báo giá (mới nhất trước), hỗ trợ tìm
        /// theo tên/SĐT và lọc theo trạng thái, có phân trang.
        /// </summary>
        /// <param name="input">Từ khóa tìm kiếm, trạng thái lọc, SkipCount/MaxResultCount phân trang.</param>
        /// <returns>Kết quả phân trang kèm tổng số bản ghi.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<PagedResultDto<QuoteRequestDto>> GetListAsync(GetQuoteRequestsInput input)
        {
            var queryable = await _quotes.GetQueryableAsync();
            System.Linq.IQueryable<QuoteRequest> query = queryable.OrderByDescending(x => x.CreatedAt);

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x =>
                    x.CustomerName.Contains(term) || x.CustomerPhone.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<QuoteStatus>(input.Status, ignoreCase: true, out var status))
            {
                query = query.Where(x => x.Status == status);
            }

            var totalCount = await _asyncExecuter.LongCountAsync(query);
            // perf-hunt 2026-10-19 (run 14:15): chặn MaxResultCount do client
            // truyền (input không có [Range]) ở server — không chặn thì 1
            // request có thể materialize toàn bộ bảng QuoteRequests.
            var items = await _asyncExecuter.ToListAsync(
                query.Skip(input.SkipCount).Take(Math.Clamp(input.MaxResultCount, 1, 200)));

            return new PagedResultDto<QuoteRequestDto>(
                totalCount,
                items.Select(MapToDto).ToList());
        }

        /// <summary>
        /// Admin đổi trạng thái xử lý của yêu cầu báo giá (vd New → Quoted → Done).
        /// </summary>
        /// <param name="id">Id yêu cầu báo giá.</param>
        /// <param name="dto">Trạng thái mới (chuỗi enum, không phân biệt hoa thường).</param>
        /// <returns>Yêu cầu sau khi đổi trạng thái.</returns>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi trạng thái không hợp lệ.</exception>
        [Authorize(Roles = "Admin")]
        public virtual async Task<QuoteRequestDto> UpdateStatusAsync(Guid id, UpdateQuoteRequestStatusDto dto)
        {
            if (!Enum.TryParse<QuoteStatus>(dto.Status, ignoreCase: true, out var status))
            {
                throw new Domain.Exceptions.DomainException($"Unknown quote status '{dto.Status}'.");
            }

            var entity = await _quotes.GetAsync(id);
            entity.Status = status;
            await _quotes.UpdateAsync(entity, autoSave: true);

            return MapToDto(entity);
        }

        /// <summary>
        /// Item 161: admin đặt số tiền cọc + thông tin chuyển khoản khi duyệt
        /// báo giá. Truyền 0 để xóa yêu cầu cọc (lưu null).
        /// </summary>
        /// <param name="id">Id yêu cầu báo giá.</param>
        /// <param name="dto">Số tiền cọc và thông tin chuyển khoản.</param>
        /// <returns>Yêu cầu sau khi cập nhật thông tin cọc.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<QuoteRequestDto> SetDepositAsync(Guid id, SetQuoteDepositDto dto)
        {
            var entity = await _quotes.GetAsync(id);
            // Truyền 0 nghĩa là "không yêu cầu cọc" → lưu null thay vì 0
            // để UI phân biệt được "chưa đặt cọc" và "cọc 0đ".
            entity.DepositAmount = dto.DepositAmount > 0 ? dto.DepositAmount : null;
            entity.DepositInfo = TrimToNull(dto.DepositInfo);
            await _quotes.UpdateAsync(entity, autoSave: true);

            return MapToDto(entity);
        }

        /// <summary>
        /// Khách đăng ký nhắc cúng hàng tháng (chỉ 2 loại: "Ram" = rằm,
        /// "Mung1" = mùng 1). Một SĐT đang bật chỉ đăng ký 1 lần — đăng ký
        /// lại thì trả về bản ghi cũ thay vì tạo trùng.
        /// </summary>
        /// <param name="dto">Tên, SĐT, loại nhắc (Ram/Mung1).</param>
        /// <returns>Đăng ký nhắc cúng (mới hoặc đã tồn tại).</returns>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi SĐT hoặc loại nhắc không hợp lệ.</exception>
        [AllowAnonymous]
        public virtual async Task<CungReminderDto> CreateReminderAsync(CreateCungReminderDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            EnsureValidVnPhone(dto.CustomerPhone);

            var type = (dto.ReminderType ?? string.Empty).Trim();
            if (!string.Equals(type, "Ram", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, "Mung1", StringComparison.OrdinalIgnoreCase))
            {
                throw new Domain.Exceptions.DomainException("Loại nhắc không hợp lệ.");
            }

            var phone = dto.CustomerPhone.Trim();
            // bug-hunt 2026-10-20: race check-then-insert — khách bấm 2 lần
            // (hoặc retry sau timeout) cùng lúc đều qua check existing == null
            // rồi cùng InsertAsync → 2 bản ghi đang bật cho cùng 1 SĐT (nhận
            // nhắc trùng), đúng cái comment idempotent muốn tránh. Serialize
            // theo 9 số cuối SĐT bằng KeyedAsyncLock (pattern ShiftClose);
            // không cần migration vì index CustomerPhone hiện tại chỉ lookup.
            return await KeyedAsyncLock.RunAsync(
                $"cung-reminder:{PhoneNumber.PhoneTailKey(phone)}", async () =>
            {
                var queryable = await _reminders.GetQueryableAsync();
                var existing = await _asyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.CustomerPhone == phone && x.IsActive));
                if (existing != null)
                {
                    // SĐT đã đăng ký nhắc và đang bật: trả về bản ghi cũ (idempotent),
                    // tránh spam nhiều đăng ký trùng cho cùng 1 SĐT.
                    return MapReminderToDto(existing);
                }

                var entity = new CungReminder
                {
                    CustomerName = dto.CustomerName.Trim(),
                    CustomerPhone = phone,
                    ReminderType = type,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _reminders.InsertAsync(entity, autoSave: true);
                return MapReminderToDto(entity);
            });
        }

        /// <summary>
        /// Admin xem đăng ký nhắc cúng, mới nhất lên trước.
        /// </summary>
        /// <returns>Danh sách đăng ký nhắc cúng (tối đa 200 bản ghi mới nhất).</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<CungReminderDto>> GetRemindersAsync()
        {
            var queryable = await _reminders.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedAt).Take(MaxReminderRows));
            return items.Select(MapReminderToDto).ToList();
        }

        /// <summary>
        /// Admin bật/tắt 1 đăng ký nhắc cúng (toggle trạng thái IsActive).
        /// </summary>
        /// <param name="id">Id đăng ký nhắc cúng.</param>
        /// <returns>Đăng ký sau khi đổi trạng thái.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<CungReminderDto> ToggleReminderAsync(Guid id)
        {
            var entity = await _reminders.GetAsync(id);
            entity.IsActive = !entity.IsActive;
            await _reminders.UpdateAsync(entity, autoSave: true);
            return MapReminderToDto(entity);
        }

        private static CungReminderDto MapReminderToDto(CungReminder entity)
        {
            return new CungReminderDto
            {
                Id = entity.Id,
                CustomerName = entity.CustomerName,
                CustomerPhone = entity.CustomerPhone,
                ReminderType = entity.ReminderType,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }

        /// <summary>
        /// Chuẩn hóa chuỗi nullable: trim, rỗng/trắng thì trả null (để DB lưu
        /// null thay vì chuỗi trắng — các field optional như địa điểm sự kiện,
        /// ngân sách, ghi chú đều dùng pattern này).
        /// </summary>
        private static string TrimToNull(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>
        /// Chặn sớm SĐT Việt Nam không hợp lệ ở cả 2 luồng public (gửi báo
        /// giá, đăng ký nhắc cúng): gom 1 chỗ để thông điệp lỗi và quy tắc
        /// validate không lệch nhau khi sửa sau này.
        /// </summary>
        /// <param name="phone">SĐT khách nhập.</param>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi SĐT sai định dạng.</exception>
        private static void EnsureValidVnPhone(string phone)
        {
            if (!PhoneNumber.IsValidVn(phone))
            {
                throw new Domain.Exceptions.DomainException("Số điện thoại không đúng định dạng (VD: 0901234567).");
            }
        }

        // perf-hunt 2026-10-19: bảng nhắc cúng tăng vô hạn theo thời gian —
        // không bao giờ materialize toàn bảng, chỉ lấy N mới nhất.
        private const int MaxReminderRows = 200;

        private static QuoteRequestDto MapToDto(QuoteRequest entity)
        {
            return new QuoteRequestDto
            {
                Id = entity.Id,
                CustomerName = entity.CustomerName,
                CustomerPhone = entity.CustomerPhone,
                EventDate = entity.EventDate,
                EventLocation = entity.EventLocation,
                Budget = entity.Budget,
                QuoteType = entity.QuoteType,
                Quantity = entity.Quantity,
                Notes = entity.Notes,
                Status = entity.Status.ToString(),
                CreatedAt = entity.CreatedAt,
                DepositAmount = entity.DepositAmount,
                DepositInfo = entity.DepositInfo
            };
        }
    }
}
