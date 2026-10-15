using PhuThuongStickyRice.Application.Catalog.DTOs;
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
    public interface IQuoteRequestAppService : IApplicationService
    {
        /// <summary>Public: customer submits a bulk/event quote request. No payment.</summary>
        Task<QuoteRequestDto> CreateAsync(CreateQuoteRequestDto dto);

        Task<PagedResultDto<QuoteRequestDto>> GetListAsync(GetQuoteRequestsInput input);

        Task<QuoteRequestDto> UpdateStatusAsync(Guid id, UpdateQuoteRequestStatusDto dto);

        /// <summary>Public: subscribes a phone to monthly cung reminders.</summary>
        Task<CungReminderDto> CreateReminderAsync(CreateCungReminderDto dto);

        /// <summary>Admin: lists cung reminder subscriptions, newest first.</summary>
        Task<List<CungReminderDto>> GetRemindersAsync();

        /// <summary>Admin: toggles a reminder subscription on/off.</summary>
        Task<CungReminderDto> ToggleReminderAsync(Guid id);
    }

    public class QuoteRequestAppService : ApplicationService, IQuoteRequestAppService
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

        [AllowAnonymous]
        public async Task<QuoteRequestDto> CreateAsync(CreateQuoteRequestDto dto)
        {
            if (!IsValidPhone(dto.CustomerPhone))
            {
                throw new Domain.Exceptions.DomainException("Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            var entity = new QuoteRequest
            {
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone.Trim(),
                EventDate = dto.EventDate?.Date,
                EventLocation = string.IsNullOrWhiteSpace(dto.EventLocation) ? null : dto.EventLocation.Trim(),
                Budget = string.IsNullOrWhiteSpace(dto.Budget) ? null : dto.Budget.Trim(),
                QuoteType = string.IsNullOrWhiteSpace(dto.QuoteType) ? null : dto.QuoteType.Trim(),
                Quantity = Math.Max(1, dto.Quantity),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Status = QuoteStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            await _quotes.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<PagedResultDto<QuoteRequestDto>> GetListAsync(GetQuoteRequestsInput input)
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
            var items = await _asyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

            return new PagedResultDto<QuoteRequestDto>(
                totalCount,
                items.Select(MapToDto).ToList());
        }

        [Authorize(Roles = "Admin")]
        public async Task<QuoteRequestDto> UpdateStatusAsync(Guid id, UpdateQuoteRequestStatusDto dto)
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

        [AllowAnonymous]
        public async Task<CungReminderDto> CreateReminderAsync(CreateCungReminderDto dto)
        {
            if (!IsValidPhone(dto.CustomerPhone))
            {
                throw new Domain.Exceptions.DomainException("Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            var type = (dto.ReminderType ?? string.Empty).Trim();
            if (!string.Equals(type, "Ram", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, "Mung1", StringComparison.OrdinalIgnoreCase))
            {
                throw new Domain.Exceptions.DomainException("Loại nhắc không hợp lệ.");
            }

            var phone = dto.CustomerPhone.Trim();
            var queryable = await _reminders.GetQueryableAsync();
            var existing = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.CustomerPhone == phone && x.IsActive));
            if (existing != null)
            {
                // Already subscribed — just return the existing one.
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
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<CungReminderDto>> GetRemindersAsync()
        {
            var queryable = await _reminders.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedAt));
            return items.Select(MapReminderToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<CungReminderDto> ToggleReminderAsync(Guid id)
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

        private static bool IsValidPhone(string phone) =>
            !string.IsNullOrWhiteSpace(phone)
            && System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(), @"^(0)(3|5|7|8|9)\d{8}$");

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
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
