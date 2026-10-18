
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
using PhuThuongStickyRice.Application.AuditLogs;

namespace PhuThuongStickyRice.Application.AuditLogs
{
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
