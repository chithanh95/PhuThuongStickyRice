using PhuThuongStickyRice.Application.EmailMessages.Services;
using PhuThuongStickyRice.CrossCuttingConcerns.CircuitBreakers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.BackgroundServer.HostedServices
{
    /// <summary>
    /// Worker nền gửi email trong hàng đợi (<c>EmailMessage</c>) ra SMTP/SendGrid.
    /// Chạy vòng quét liên tục: có mail thì gửi ngay, hết mail thì nghỉ
    /// <c>IdlePollDelay</c> rồi quét lại. Lỗi thoáng qua (mất DB, SMTP chết,
    /// circuit breaker mở) chỉ bị log rồi thử lại vòng sau — worker không bao
    /// giờ tự tắt để 1 sự cố tạm thời không làm mất hàng đợi mail.
    /// </summary>
    public class SendEmailWorker : BackgroundService
    {
        // Khoảng chờ giữa 2 vòng quét hàng đợi email khi không có mail nào
        // được gửi (tránh quay vòng CPU vô ích).
        private static readonly TimeSpan IdlePollDelay = TimeSpan.FromSeconds(10);

        private readonly IServiceProvider _services;
        private readonly ILogger<SendEmailWorker> _logger;

        public SendEmailWorker(IServiceProvider services,
            ILogger<SendEmailWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("SendEmailService is starting.");
            await SendPendingEmailsLoop(stoppingToken);
        }

        private async Task SendPendingEmailsLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("SendEmail task doing background work.");

                int sentCount = 0;

                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var emailService = scope.ServiceProvider.GetRequiredService<EmailMessageService>();

                        sentCount = await emailService.SendEmailMessagesAsync();
                    }

                    if (sentCount == 0)
                    {
                        await Task.Delay(IdlePollDelay, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Host đang dừng (Task.Delay bị hủy): thoát vòng lặp, đây là
                    // shutdown bình thường chứ không phải lỗi.
                    break;
                }
                catch (CircuitBreakerOpenException)
                {
                    if (await WaitBeforeNextPollAsync(stoppingToken))
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Lỗi thoáng qua (mất kết nối DB, SMTP chết...): log rồi thử
                    // lại ở vòng sau — không để 1 lần lỗi giết chết worker vĩnh
                    // viễn (cùng pattern với WeeklyReportWorker).
                    _logger.LogError(ex, "SendEmailWorker failed; will retry on the next poll.");
                    if (await WaitBeforeNextPollAsync(stoppingToken))
                    {
                        break;
                    }
                }
            }

            _logger.LogDebug("SendEmail background task is stopping.");
        }

        // [codequality-practices 2026-10-20] Nghỉ giữa 2 vòng quét an toàn khi
        // host dừng: Task.Delay(..., stoppingToken) ném OperationCanceledException
        // nếu host dừng đúng lúc đang chờ. Exception ném BÊN TRONG một catch
        // handler không bị bắt lại bởi các handler khác của cùng khối try —
        // trước đây nó thoát luôn ExecuteAsync khiến worker fault (ghi nhận
        // lỗi) thay vì dừng êm như shutdown bình thường. Helper này nuốt đúng
        // tín hiệu hủy và trả true để vòng lặp break; hành vi "lỗi thì nghỉ
        // rồi quét lại" giữ nguyên.
        private static async Task<bool> WaitBeforeNextPollAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(IdlePollDelay, stoppingToken);
                return false;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return true;
            }
        }
    }
}