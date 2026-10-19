using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PhuThuongStickyRice.Infrastructure.Logging
{
    public static class LoggingExtensions
    {
        /// <summary>Dung lượng tối đa của mỗi file log trước khi roll sang file mới (10 MB).</summary>
        private const long LogFileSizeLimitBytes = 10L * 1024 * 1024;

        /// <summary>Chu kỳ flush buffer log xuống đĩa.</summary>
        private static readonly TimeSpan LogFlushToDiskInterval = TimeSpan.FromSeconds(1);

        /// <summary>Tên file log chính trong thư mục logs.</summary>
        private const string LogFileName = "log.txt";

        /// <summary>Tên file hứng các event Elasticsearch gửi thất bại.</summary>
        private const string ElasticsearchFailureLogFileName = "elasticsearch-failures.txt";

        /// <summary>Hậu tố ngày tháng ghép vào tên index Elasticsearch (index theo ngày).</summary>
        private const string ElasticsearchIndexDateSuffix = "-{0:yyyy.MM.dd}";

        private static void UsePhuThuongStickyRiceLogger(this IWebHostEnvironment env, LoggingOptions options)
        {
            var loggerConfiguration = CreateBaseLoggerConfiguration(
                    env.ContentRootPath, env.ApplicationName, env.EnvironmentName)
                .Enrich.WithProperty("WebRootPath", env.WebRootPath);

            ApplyFileAndElasticsearchSinks(loggerConfiguration, env.ContentRootPath, options);

            Log.Logger = loggerConfiguration.CreateLogger();
        }

        /// <summary>
        /// Dựng phần chung của cấu hình Serilog (mức log, enricher, thư mục logs).
        /// Dùng chung cho cả host web (<see cref="IWebHostEnvironment"/>) và host
        /// thường (<see cref="IHostEnvironment"/>) để không trùng lặp code.
        /// </summary>
        private static LoggerConfiguration CreateBaseLoggerConfiguration(
            string contentRootPath, string applicationName, string environmentName)
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

            var logsPath = Path.Combine(contentRootPath, "logs");
            Directory.CreateDirectory(logsPath);

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.With<ActivityEnricher>()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithProperty("Assembly", assemblyName)
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("EnvironmentName", environmentName)
                .Enrich.WithProperty("ContentRootPath", contentRootPath);
        }

        /// <summary>
        /// Gắn sink ghi file log + sink Elasticsearch (nếu bật) vào cấu hình logger.
        /// </summary>
        private static void ApplyFileAndElasticsearchSinks(
            LoggerConfiguration loggerConfiguration, string contentRootPath, LoggingOptions options)
        {
            var logsPath = Path.Combine(contentRootPath, "logs");

            loggerConfiguration
                .Enrich.WithExceptionDetails()
                .Filter.ByIncludingOnly((logEvent) =>
                {
                    if (logEvent.Level >= options.File.MinimumLogEventLevel
                    || logEvent.Level >= options.Elasticsearch.MinimumLogEventLevel)
                    {
                        var sourceContext = logEvent.Properties.ContainsKey("SourceContext")
                             ? logEvent.Properties["SourceContext"].ToString()
                             : null;

                        var logLevel = GetLogLevel(sourceContext, options);

                        return logEvent.Level >= logLevel;
                    }

                    return false;
                })
                .WriteTo.File(Path.Combine(logsPath, LogFileName),
                    fileSizeLimitBytes: LogFileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: LogFlushToDiskInterval,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [TraceId: {TraceId}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: options.File.MinimumLogEventLevel);

            if (options.Elasticsearch != null && options.Elasticsearch.IsEnabled)
            {
                loggerConfiguration
                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(options.Elasticsearch.Host))
                    {
                        MinimumLogEventLevel = options.Elasticsearch.MinimumLogEventLevel,
                        AutoRegisterTemplate = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                        IndexFormat = options.Elasticsearch.IndexFormat + ElasticsearchIndexDateSuffix,
                        // BufferBaseFilename = Path.Combine(contentRootPath, "logs", "buffer"),
                        InlineFields = true,
                        EmitEventFailure = EmitEventFailureHandling.WriteToFailureSink,
                        FailureSink = new FileSink(Path.Combine(logsPath, ElasticsearchFailureLogFileName), new JsonFormatter(), null),
                    });
            }
        }

        private static LoggingOptions SetDefault(LoggingOptions options)
        {
            options ??= new LoggingOptions
            {
            };

            options.LogLevel ??= new Dictionary<string, string>();

            if (!options.LogLevel.ContainsKey("Default"))
            {
                options.LogLevel["Default"] = "Warning";
            }

            options.File ??= new FileOptions
            {
                MinimumLogEventLevel = Serilog.Events.LogEventLevel.Warning,
            };

            options.Elasticsearch ??= new ElasticsearchOptions
            {
                IsEnabled = false,
                MinimumLogEventLevel = Serilog.Events.LogEventLevel.Warning,
            };

            options.EventLog ??= new EventLogOptions
            {
                IsEnabled = false,
            };
            return options;
        }

        private static Serilog.Events.LogEventLevel GetLogLevel(string context, LoggingOptions options)
        {
            context = context.Replace("\"", string.Empty);
            string level = "Default";
            var matches = options.LogLevel.Keys.Where(k => context.StartsWith(k, StringComparison.OrdinalIgnoreCase));

            if (matches.Any())
            {
                level = matches.Max();
            }

            return (Serilog.Events.LogEventLevel)Enum.Parse(typeof(Serilog.Events.LogEventLevel), options.LogLevel[level], true);
        }

        public static IWebHostBuilder UsePhuThuongStickyRiceLogger(this IWebHostBuilder builder, Func<IConfiguration, LoggingOptions> logOptions)
        {
            builder.ConfigureLogging((context, logging) =>
            {
                logging.Configure(options =>
                {
                    // options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
                });

                logging.AddAzureWebAppDiagnostics();

                logging.AddSerilog();

                LoggingOptions options = SetDefault(logOptions(context.Configuration));

                if (options.EventLog != null && options.EventLog.IsEnabled && OperatingSystem.IsWindows())
                {
                    logging.AddEventLog(new EventLogSettings
                    {
                        LogName = options.EventLog.LogName,
                        SourceName = options.EventLog.SourceName,
                    });
                }

                if (options?.ApplicationInsights?.IsEnabled ?? false)
                {
                    logging.AddApplicationInsights(
                        configureTelemetryConfiguration: (config) =>
                        {
                            config.ConnectionString = options.ApplicationInsights.InstrumentationKey;
                        },
                        configureApplicationInsightsLoggerOptions: (options) => { });
                }

                if (options?.OpenTelemetry?.IsEnabled ?? false)
                {
                    var resourceBuilder = ResourceBuilder.CreateDefault().AddService(options.OpenTelemetry.ServiceName);

                    logging.AddOpenTelemetry(configure =>
                    {
                        configure.SetResourceBuilder(resourceBuilder)
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(options.OpenTelemetry.Otlp.Endpoint);
                        });
                    });
                }

                context.HostingEnvironment.UsePhuThuongStickyRiceLogger(options);
            });

            return builder;
        }

        public static IHostBuilder UsePhuThuongStickyRiceLogger(this IHostBuilder builder, Func<IConfiguration, LoggingOptions> logOptions)
        {
            builder.ConfigureLogging((context, logging) =>
            {
                logging.Configure(options =>
                {
                    // options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
                });

                logging.AddAzureWebAppDiagnostics();

                logging.AddSerilog();

                LoggingOptions options = SetDefault(logOptions(context.Configuration));

                if (options.EventLog != null && options.EventLog.IsEnabled && OperatingSystem.IsWindows())
                {
                    logging.AddEventLog(new EventLogSettings
                    {
                        LogName = options.EventLog.LogName,
                        SourceName = options.EventLog.SourceName,
                    });
                }

                if (options?.ApplicationInsights?.IsEnabled ?? false)
                {
                    logging.AddApplicationInsights(
                        configureTelemetryConfiguration: (config) =>
                        {
                            config.ConnectionString = options.ApplicationInsights.InstrumentationKey;
                        },
                        configureApplicationInsightsLoggerOptions: (options) => { });
                }

                if (options?.OpenTelemetry?.IsEnabled ?? false)
                {
                    var resourceBuilder = ResourceBuilder.CreateDefault().AddService(options.OpenTelemetry.ServiceName);

                    logging.AddOpenTelemetry(configure =>
                    {
                        configure.SetResourceBuilder(resourceBuilder)
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(options.OpenTelemetry.Otlp.Endpoint);
                        });
                    });
                }

                context.HostingEnvironment.UsePhuThuongStickyRiceLogger(options);
            });

            return builder;
        }

        private static void UsePhuThuongStickyRiceLogger(this IHostEnvironment env, LoggingOptions options)
        {
            var loggerConfiguration = CreateBaseLoggerConfiguration(
                env.ContentRootPath, env.ApplicationName, env.EnvironmentName);

            ApplyFileAndElasticsearchSinks(loggerConfiguration, env.ContentRootPath, options);

            Log.Logger = loggerConfiguration.CreateLogger();
        }
    }
}
