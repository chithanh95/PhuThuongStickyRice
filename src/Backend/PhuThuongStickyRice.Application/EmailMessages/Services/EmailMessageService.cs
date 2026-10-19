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
        private readonly ICircuitBreakerManager _circuitBreakerManager;
        private readonly IDistributedLock _distributedLock;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public EmailMessageService(ILogger<EmailMessageService> logger,
            IEmailMessageRepository repository,
            IEmailNotification emailNotification,
            IDateTimeProvider dateTimeProvider,
            ICircuitBreakerManager circuitBreakerManager,
            IDistributedLock distributedLock,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _logger = logger;
            _repository = repository;
            _emailNotification = emailNotification;
            _dateTimeProvider = dateTimeProvider;
            _circuitBreakerManager = circuitBreakerManager;
            _distributedLock = distributedLock;
            _asyncExecuter = asyncExecuter;
        }

        public async Task<int> SendEmailMessagesAsync()
        {
            var circuit = _circuitBreakerManager.GetCircuitBreaker(EmailCircuitBreakerName, CircuitOpenDuration);
            circuit.EnsureOkStatus();

            var dateTime = _dateTimeProvider.OffsetNow;

            var messages = await _asyncExecuter.ToListAsync(
                _repository.GetAll()
                    .Where(x => x.SentDateTime == null)
                    .Where(x => x.ExpiredDateTime == null || x.ExpiredDateTime > dateTime)
                    .Where(x => (x.MaxAttemptCount == 0 && x.AttemptCount < DefaultAttemptCount) || x.AttemptCount < x.MaxAttemptCount)
                    .Where(x => x.NextAttemptDateTime == null || x.NextAttemptDateTime <= dateTime));

            if (messages.Any())
            {
                foreach (var email in messages)
                {
                    await SendOneAsync(email, circuit);
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

                _circuitBreakerManager.LogSuccess(circuit);
            }
            catch (Exception ex)
            {
                email.Log += log + ex.ToString();
                email.NextAttemptDateTime = _dateTimeProvider.OffsetNow + RetryDelays[email.AttemptCount];

                _circuitBreakerManager.LogFailure(circuit, CircuitFailureThreshold, TimeSpan.FromMinutes(5));
            }

            email.AttemptCount += 1;
            email.Log = email.Log.Trim();
            email.UpdatedDateTime = _dateTimeProvider.OffsetNow;

            if (email.MaxAttemptCount == 0)
            {
                email.MaxAttemptCount = DefaultAttemptCount;
            }

            await _repository.UnitOfWork.SaveChangesAsync();

            circuit.EnsureOkStatus();
        }
    }
}
