
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

            // [codequality-smell 2026-10-19] Magic number: kích thước trang log
            // mặc định và tối đa — đặt hằng số có tên để thay đổi 1 chỗ.
            private const int DefaultPageSize = 50;
            private const int MaxPageSize = 200;

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
                IQueryable<AppLog> query = queryable.OrderByDescending(x => x.At);

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
                        input.MaxResultCount < 1 ? DefaultPageSize : Math.Min(input.MaxResultCount, MaxPageSize)));

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

    /// <summary>
    /// Ghi log ứng dụng vào bảng AppLogs: chỉ giữ 2000 dòng mới nhất
    /// (tự cắt dòng cũ mỗi lần ghi) để bảng log không phình vô hạn.
    /// Không bao giờ ghi log của chính writer và của webhook — nếu không sẽ
    /// đệ quy vô hạn (ghi log → gọi writer → ghi log...).
    /// </summary>
    public class AppLogWriter : IAppLogWriter
    {
        /// <summary>Số dòng log tối đa giữ lại trong bảng AppLogs.</summary>
        public const int MaxRows = 2000;

        /// <summary>
        /// Chu kỳ cắt dòng cũ: chỉ prune mỗi N lần ghi thay vì mỗi lần ghi.
        /// Caller duy nhất hiện nay là endpoint public báo lỗi client (đã
        /// rate-limit), nên bảng phình thêm tối đa ~N dòng giữa 2 lần prune.
        /// </summary>
        private const int PruneEveryNWrites = 100;

        /// <summary>Đếm số lần ghi toàn process để quyết định kỳ prune.</summary>
        private static int _writesSincePrune;

        private readonly IRepository<AppLog, Guid> _logs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Volo.Abp.Timing.IClock _clock;

        /// <summary>Khởi tạo writer ghi log vào bảng AppLogs.</summary>
        /// <param name="logs">Repository bảng AppLog.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn bất đồng bộ.</param>
        /// <param name="clock">Đồng hồ lấy thời gian hiện tại.</param>
        public AppLogWriter(
            IRepository<AppLog, Guid> logs,
            IAsyncQueryableExecuter asyncExecuter,
            Volo.Abp.Timing.IClock clock)
        {
            _logs = logs;
            _asyncExecuter = asyncExecuter;
            _clock = clock;
        }

        /// <summary>
        /// Ghi một dòng log (xem doc ở interface). Bỏ qua log của chính
        /// writer/webhook để tránh đệ quy; cắt Source/Message theo độ dài
        /// tối đa của cột trước khi lưu.
        /// </summary>
        /// <param name="level">Mức log (vd Warning, Error).</param>
        /// <param name="source">Nguồn phát log (tên class/component).</param>
        /// <param name="message">Nội dung log.</param>
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
                    : source[..AppLog.SourceMaxLength],
                Message = message == null || message.Length <= AppLog.MessageMaxLength
                    ? message
                    : message[..AppLog.MessageMaxLength]
            }, autoSave: true);

            // Prune: keep only the latest MaxRows.
            // perf-hunt 2026-10-19: prune theo chu kỳ (mỗi 100 lần ghi) thay vì
            // chạy 3 round-trip trên MỖI lần ghi; mỗi lần prune chỉ xóa số dòng
            // dư (~100 dòng) thay vì quét/cân nhắc xóa trên mọi lần ghi.
            // Vẫn giữ tối đa ~2000 dòng mới nhất.
            if (Interlocked.Increment(ref _writesSincePrune) % PruneEveryNWrites == 0)
            {
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
}
