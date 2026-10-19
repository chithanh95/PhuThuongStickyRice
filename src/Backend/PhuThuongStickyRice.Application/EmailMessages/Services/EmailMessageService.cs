using PhuThuongStickyRice.CrossCuttingConcerns.CircuitBreakers;
using PhuThuongStickyRice.CrossCuttingConcerns.Locks;
using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Notification;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.EmailMessages.Services
{
    public class EmailMessageService
    {
        // Tên circuit breaker bảo vệ SMTP (tránh spam server mail khi lỗi liên tục).
        private const string EmailCircuitBreakerName = "EmailService";

        // Số lần thử mặc định khi email không khai MaxAttemptCount.
        private const int DefaultAttemptCount = 5;

        // Ngưỡng ghi nhận circuit breaker mở khi gửi lỗi liên tục.
        private const int CircuitFailureThreshold = 5;
        private static readonly TimeSpan CircuitOpenDuration = TimeSpan.FromMinutes(1);

        // Lịch chờ thử lại theo số lần đã thử (Fibonacci phút: 1, 2, 3, 5, ...).
        // Độ dài tối đa của email.Log — mỗi lần gửi lỗi cũ cộng dồn cả
        // ex.ToString() (full stack trace) qua nhiều lần retry sẽ phình
        // vô hạn. Chỉ giữ phần mới nhất.
        private const int MaxLogLength = 4000;
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(8),
            TimeSpan.FromMinutes(13),
            TimeSpan.FromMinutes(21),
            TimeSpan.FromMinutes(34),
            TimeSpan.FromMinutes(55),
            TimeSpan.FromMinutes(89),
        };

        private readonly ILogger _logger;
        private readonly IEmailMessageRepository _repository;
        private readonly IEmailNotification _emailNotification;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ICircuitBreakerStore _circuitBreakerStore;
        private readonly IDistributedLock _distributedLock;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public EmailMessageService(ILogger<EmailMessageService> logger,
            IEmailMessageRepository repository,
            IEmailNotification emailNotification,
            IDateTimeProvider dateTimeProvider,
            ICircuitBreakerStore circuitBreakerStore,
            IDistributedLock distributedLock,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _logger = logger;
            _repository = repository;
            _emailNotification = emailNotification;
            _dateTimeProvider = dateTimeProvider;
            _circuitBreakerStore = circuitBreakerStore;
            _distributedLock = distributedLock;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Gửi các email đang chờ (chạy mỗi 10s bởi SendEmailWorker).
        /// perf-hunt 2026-10-19: lấy theo từng đợt tối đa 100 mail (ưu tiên
        /// mail đến hạn trước — NULL NextAttemptDateTime xếp đầu khi sắp
        /// tăng dần) thay vì nạp toàn bộ hàng đợi vào bộ nhớ; nếu SMTP chết
        /// lâu, hàng đợi phình to sẽ không còn làm mỗi vòng lặp nặng dần.
        /// Worker quay lại ngay khi còn mail (rs &gt; 0) nên các đợt sau vẫn
        /// được gửi tiếp, hành vi không đổi. Trạng thái các mail được lưu
        /// 1 lần cuối đợt (try/finally) thay vì SaveChanges sau mỗi mail.
        /// </summary>
        /// <returns>Số mail đã xử lý trong đợt này.</returns>
        public async Task<int> SendEmailMessagesAsync()
        {
            const int BatchSize = 100;
            var circuit = _circuitBreakerStore.GetCircuitBreaker(EmailCircuitBreakerName, CircuitOpenDuration);
            circuit.EnsureOkStatus();

            var dateTime = _dateTimeProvider.OffsetNow;

            var messages = await _asyncExecuter.ToListAsync(
                _repository.GetAll()
                    .Where(x => x.SentDateTime == null)
                    .Where(x => x.ExpiredDateTime == null || x.ExpiredDateTime > dateTime)
                    .Where(x => (x.MaxAttemptCount == 0 && x.AttemptCount < DefaultAttemptCount) || x.AttemptCount < x.MaxAttemptCount)
                    .Where(x => x.NextAttemptDateTime == null || x.NextAttemptDateTime <= dateTime)
                    .OrderBy(x => x.NextAttemptDateTime)
                    .Take(BatchSize));

            if (messages.Count > 0)
            {
                try
                {
                    foreach (var email in messages)
                    {
                        await SendOneAsync(email, circuit);
                    }
                }
                finally
                {
                    // perf-hunt 2026-10-19: gom 1 lần SaveChanges/đợt thay vì
                    // 1 lần/mail (tối đa 100 roundtrip DB mỗi 10 giây).
                    // finally để trạng thái các mail đã xử lý vẫn được lưu khi
                    // circuit mở giữa đợt (bản cũ đã save từng mail trước khi
                    // EnsureOkStatus ném) — hành vi "mỗi mail độc lập" không đổi.
                    await _repository.UnitOfWork.SaveChangesAsync();
                }
            }
            else
            {
                _logger.LogInformation("No email to send.");
            }

            return messages.Count;
        }

        /// <summary>
        /// Gửi một email: thành công ghi log + đóng circuit tốt, thất bại
        /// hẹn giờ thử lại theo lịch Fibonacci và mở circuit nếu lỗi liên tục.
        /// </summary>
        private async Task SendOneAsync(EmailMessage email, ICircuitBreaker circuit)
        {
            string log = Environment.NewLine + Environment.NewLine
                    + $"[{_dateTimeProvider.OffsetNow.ToString(CultureInfo.InvariantCulture)}] ";
            try
            {
                await _emailNotification.SendAsync(email);
                email.SentDateTime = _dateTimeProvider.OffsetNow;
                email.Log += log + "Succeed.";

                _circuitBreakerStore.LogSuccess(circuit);
            }
            catch (Exception ex)
            {
                // perf-hunt 2026-10-19: chỉ ghi ex.Message (bản cũ cộng dồn
                // ex.ToString() — full stack trace — qua nhiều lần retry làm
                // cột Log phình vô hạn); TruncateLog bên dưới giữ phần mới nhất.
                email.Log += log + ex.Message;

                // Guard: RetryDelays chỉ có 10 phần tử trong khi AttemptCount
                // có thể vượt 10 khi MaxAttemptCount đặt lớn — bản cũ ném
                // IndexOutOfRange làm chết cả vòng lặp batch.
                email.NextAttemptDateTime = _dateTimeProvider.OffsetNow
                    + RetryDelays[Math.Min(email.AttemptCount, RetryDelays.Length - 1)];

                _circuitBreakerStore.LogFailure(circuit, CircuitFailureThreshold, TimeSpan.FromMinutes(5));
            }

            email.AttemptCount += 1;

            // perf-hunt 2026-10-19: cắt Log giữ tối đa MaxLogLength ký tự
            // mới nhất — chặn phình vô hạn qua các lần retry.
            email.Log = TruncateLog(email.Log.Trim());
            email.UpdatedDateTime = _dateTimeProvider.OffsetNow;

            if (email.MaxAttemptCount == 0)
            {
                email.MaxAttemptCount = DefaultAttemptCount;
            }

            circuit.EnsureOkStatus();
        }

        /// <summary>Giữ lại tối đa <see cref="MaxLogLength"/> ký tự mới nhất của log.</summary>
        private static string TruncateLog(string log) =>
            string.IsNullOrEmpty(log) || log.Length <= MaxLogLength
                ? log
                : log[^MaxLogLength..];
    }
}
