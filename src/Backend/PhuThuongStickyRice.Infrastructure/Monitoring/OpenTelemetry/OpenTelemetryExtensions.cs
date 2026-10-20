using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Reflection;

namespace PhuThuongStickyRice.Infrastructure.Monitoring.OpenTelemetry
{
    /// <summary>
    /// Đăng ký OpenTelemetry (tracing + metrics) cho host. Khi tắt trong cấu
    /// hình (<c>OpenTelemetryOptions.IsEnabled</c> = false hoặc options null)
    /// thì không đăng ký gì — app vẫn chạy bình thường, chỉ không có telemetry.
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        /// <summary>
        /// Đăng ký OpenTelemetry cho service collection: tracing (ASP.NET Core,
        /// EF Core, HttpClient; exporter Jaeger/Zipkin/OTLP theo cấu hình) và
        /// metrics (runtime, process, ASP.NET Core, HttpClient; exporter OTLP).
        /// Mỗi instance gắn tên máy làm service instance để phân biệt các
        /// replica trên dashboard tập trung.
        /// </summary>
        /// <param name="services">Service collection của host.</param>
        /// <param name="options">Cấu hình telemetry; null hoặc tắt thì bỏ qua.</param>
        /// <returns>Service collection để chain tiếp.</returns>
        public static IServiceCollection AddPhuThuongStickyRiceOpenTelemetry(this IServiceCollection services, OpenTelemetryOptions options = null)
        {
            if (options == null || !options.IsEnabled)
            {
                return services;
            }

            var resourceBuilder = ResourceBuilder.CreateDefault().AddService(options.ServiceName);

            services.AddOpenTelemetry()
                .ConfigureResource(configureResource =>
                {
                    configureResource.AddService(
                        serviceName: options.ServiceName,
                        serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                        serviceInstanceId: Environment.MachineName);
                })
                .WithTracing(builder =>
                {
                    builder
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                    if (options?.Jaeger?.IsEnabled ?? false)
                    {
                        builder.AddJaegerExporter(jaegerOptions =>
                         {
                             jaegerOptions.AgentHost = options.Jaeger.AgentHost;
                             jaegerOptions.AgentPort = options.Jaeger.AgentPort;
                         });
                    }

                    if (options?.Zipkin?.IsEnabled ?? false)
                    {
                        builder.AddZipkinExporter(zipkinOptions =>
                        {
                            zipkinOptions.Endpoint = new Uri(options.Zipkin.Endpoint);
                        });
                    }

                    if (options?.Otlp?.IsEnabled ?? false)
                    {
                        builder.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(options.Otlp.Endpoint);
                        });
                    }
                })
                .WithMetrics(builder =>
                {
                    builder
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                    if (options?.Otlp?.IsEnabled ?? false)
                    {
                        builder.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(options.Otlp.Endpoint);
                        });
                    }
                })
                .StartWithHost();

            return services;
        }
    }
}
