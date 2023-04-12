using PhuThuongStickyRice.Application.FileEntries.DTOs;
using PhuThuongStickyRice.BackgroundServer.ConfigurationOptions;
using PhuThuongStickyRice.BackgroundServer.HostedServices;
using PhuThuongStickyRice.BackgroundServer.Identity;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace PhuThuongStickyRice.BackgroundServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UsePhuThuongStickyRiceLogger(configuration =>
            {
                var appSettings = new AppSettings();
                configuration.Bind(appSettings);
                return appSettings.Logging;
            })
            .ConfigureServices((hostContext, services) =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var configuration = serviceProvider.GetService<IConfiguration>();

                var appSettings = new AppSettings();
                configuration.Bind(appSettings);

                var validationResult = appSettings.Validate();
                if (validationResult.Failed)
                {
                    throw new Exception(validationResult.FailureMessage);
                }

                services.Configure<AppSettings>(configuration);

                services.AddScoped<ICurrentUser, CurrentUser>();

                services.AddDateTimeProvider();
                services.AddPersistence(appSettings.ConnectionStrings.PhuThuongStickyRice)
                        .AddDomainServices()
                        .AddApplicationServices();

                services.AddMessageBusSender<FileUploadedEvent>(appSettings.MessageBroker);
                services.AddMessageBusSender<FileDeletedEvent>(appSettings.MessageBroker);

                services.AddMessageBusReceiver<FileUploadedEvent>(appSettings.MessageBroker);
                services.AddMessageBusReceiver<FileDeletedEvent>(appSettings.MessageBroker);

                services.AddNotificationServices(appSettings.Notification);

                services.AddWebNotification<SendTaskStatusMessage>(appSettings.Notification.Web);

                services.AddHostedService<MessageBusReceiver>();
                services.AddHostedService<PublishEventWorker>();
                services.AddHostedService<SendEmailWorker>();
                services.AddHostedService<SendSmsWorker>();
                services.AddHostedService<ScheduleCronJobWorker>();
            });
    }
}
