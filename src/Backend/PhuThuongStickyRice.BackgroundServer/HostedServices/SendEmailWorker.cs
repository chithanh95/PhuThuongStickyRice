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
                catch (CircuitBreakerOpenException)
                {
                    await Task.Delay(IdlePollDelay, stoppingToken);
                }
            }

            _logger.LogDebug("SendEmail background task is stopping.");
        }
    }
}