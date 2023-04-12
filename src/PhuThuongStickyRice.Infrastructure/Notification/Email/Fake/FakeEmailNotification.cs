﻿using PhuThuongStickyRice.Domain.Notification;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Notification.Email.SmtpClient
{
    public class FakeEmailNotification : IEmailNotification
    {
        public Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}