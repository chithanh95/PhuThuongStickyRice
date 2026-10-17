using PhuThuongStickyRice.Application.Wave4.DTOs;
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

namespace PhuThuongStickyRice.Application.Wave4
{
    public interface IAddressBookAppService : IApplicationService
    {
        /// <summary>Public: saved addresses for a phone.</summary>
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
            var entities = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.Phone == normalized)
                    .OrderByDescending(x => x.CreatedDateTime));
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

    public interface IAppLogAppService : IApplicationService
    {
        /// <summary>Admin: paged log viewer with level filter + search.</summary>
        Task<PagedResultDto<AppLogDto>> GetListAsync(GetAppLogsInput input);
    }

    public class AppLogAppService : ApplicationService, IAppLogAppService
    {
        private readonly IRepository<AppLog, Guid> _logs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public AppLogAppService(
            IRepository<AppLog, Guid> logs,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _logs = logs;
            _asyncExecuter = asyncExecuter;
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<PagedResultDto<AppLogDto>> GetListAsync(GetAppLogsInput input)
        {
            var queryable = await _logs.GetQueryableAsync();
            System.Linq.IQueryable<AppLog> query = queryable.OrderByDescending(x => x.At);

            if (!string.IsNullOrWhiteSpace(input.Level))
            {
                var level = input.Level.Trim();
                query = query.Where(x => x.Level == level);
            }

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x => x.Message.Contains(term) || x.Source.Contains(term));
            }

            var total = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(
                query.Skip(Math.Max(0, input.SkipCount)).Take(
                    input.MaxResultCount < 1 ? 50 : Math.Min(input.MaxResultCount, 200)));

            return new PagedResultDto<AppLogDto>(total, items.Select(x => new AppLogDto
            {
                Id = x.Id,
                At = x.At,
                Level = x.Level,
                Source = x.Source,
                Message = x.Message
            }).ToList());
        }
    }

    /// <summary>
    /// Lightweight logging hook: persists Warning+ entries to the AppLogs
    /// table (capped at the latest 2000 rows). Registered as an
    /// ILoggerProvider in the WebAPI Startup.
    /// </summary>
    public interface IAppLogWriter : Volo.Abp.DependencyInjection.ITransientDependency
    {
        Task WriteAsync(string level, string source, string message);
    }

    public class AppLogWriter : IAppLogWriter
    {
        public const int MaxRows = 2000;

        private readonly IRepository<AppLog, Guid> _logs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Volo.Abp.Timing.IClock _clock;

        public AppLogWriter(
            IRepository<AppLog, Guid> logs,
            IAsyncQueryableExecuter asyncExecuter,
            Volo.Abp.Timing.IClock clock)
        {
            _logs = logs;
            _asyncExecuter = asyncExecuter;
            _clock = clock;
        }

        public virtual async Task WriteAsync(string level, string source, string message)
        {
            // Never log the writer itself — that would recurse forever.
            if (!string.IsNullOrEmpty(source)
                && (source.Contains("AppLog") || source.Contains("Webhook")))
            {
                return;
            }

            await _logs.InsertAsync(new AppLog
            {
                Id = Guid.NewGuid(),
                At = _clock.Now,
                Level = level,
                Source = source == null || source.Length <= AppLog.SourceMaxLength
                    ? source
                    : source.Substring(0, AppLog.SourceMaxLength),
                Message = message == null || message.Length <= AppLog.MessageMaxLength
                    ? message
                    : message.Substring(0, AppLog.MessageMaxLength)
            }, autoSave: true);

            // Prune: keep only the latest MaxRows.
            var queryable = await _logs.GetQueryableAsync();
            var cutoff = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.OrderByDescending(x => x.At).Skip(MaxRows - 1).Take(1));
            if (cutoff != null)
            {
                var old = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.At < cutoff.At));
                if (old.Count > 0)
                {
                    await _logs.DeleteManyAsync(old, autoSave: true);
                }
            }
        }
    }
}
