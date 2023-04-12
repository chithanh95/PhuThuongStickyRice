﻿using PhuThuongStickyRice.WebMVC.ConfigurationOptions;
using PhuThuongStickyRice.WebMVC.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace PhuThuongStickyRice.WebMVC.Configurations
{
    public static class SignalRConfiguration
    {
        public static IServiceCollection AddPhuThuongStickyRiceSignalR(this IServiceCollection services, AppSettings appSettings)
        {
            var signalR = services.AddSignalR();

            if (appSettings.Azure?.SignalR?.IsEnabled ?? false)
            {
                signalR.AddAzureSignalR();
            }

            signalR.AddMessagePackProtocol();

            return services;
        }

        public static void MapPhuThuongStickyRiceHubs(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<AuthorizedHub>("/AuthorizedHub");
            endpoints.MapHub<HealthCheckHub>("/HealthCheckHub");
            endpoints.MapHub<SimulatedLongRunningTaskHub>("/SimulatedLongRunningTaskHub");
        }
    }
}
