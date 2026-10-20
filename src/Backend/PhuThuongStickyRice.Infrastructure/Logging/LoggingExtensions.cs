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
            // Host web enrich thêm WebRootPath rồi dùng chung pipeline của host thường.
            UsePhuThuongStickyRiceLogger((IHostEnvironment)env, options,
                loggerConfiguration => loggerConfiguration.Enrich.WithProperty("WebRootPath", env.WebRootPath));
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

            ApplySharedLogFilter(loggerConfiguration, options);
            ApplyFileSink(loggerConfiguration, logsPath, options);
            ApplyElasticsearchSink(loggerConfiguration, logsPath, options);
        }

        /// <summary>
        /// Filter chung cho mọi sink: chỉ giữ log event đạt mức tối thiểu của
        /// file sink hoặc Elasticsearch sink, rồi áp mức chi tiết theo SourceContext.
        /// </summary>
        private static void ApplySharedLogFilter(LoggerConfiguration loggerConfiguration, LoggingOptions options)
        {
            loggerConfiguration
                .Enrich.WithExceptionDetails()
                .Filter.ByIncludingOnly((logEvent) =>
                {
                    if (logEvent.Level >= options.File.MinimumLogEventLevel
                    || logEvent.Level >= options.Elasticsearch.MinimumLogEventLevel)
                    {
                        // CA1854: TryGetValue 1 lần thay vì ContainsKey + indexer
                        // (filter chạy trên mọi log event).
                        var sourceContext = logEvent.Properties.TryGetValue("SourceContext", out var prop)
                             ? prop.ToString()
                             : null;

                        var logLevel = GetLogLevel(sourceContext, options);

                        return logEvent.Level >= logLevel;
                    }

                    return false;
                });
        }

        /// <summary>Gắn sink ghi log ra file log.txt (tự roll khi vượt dung lượng).</summary>
        private static void ApplyFileSink(LoggerConfiguration loggerConfiguration, string logsPath, LoggingOptions options)
        {
            loggerConfiguration
                .WriteTo.File(Path.Combine(logsPath, LogFileName),
                    fileSizeLimitBytes: LogFileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: LogFlushToDiskInterval,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [TraceId: {TraceId}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: options.File.MinimumLogEventLevel);
        }

        /// <summary>Gắn sink Elasticsearch khi được bật trong cấu hình.</summary>
        private static void ApplyElasticsearchSink(LoggerConfiguration loggerConfiguration, string logsPath, LoggingOptions options)
        {
            if (options.Elasticsearch == null || !options.Elasticsearch.IsEnabled)
            {
                return;
            }

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
                    // FileSink ctor đã obsolete: dựng sink qua WriteTo.File()
                    // (cùng JsonFormatter, cùng 1 file không giới hạn dung
                    // lượng như trước) — Logger implements ILogEventSink.
                    FailureSink = new LoggerConfiguration()
                        .WriteTo.File(new JsonFormatter(),
                            Path.Combine(logsPath, ElasticsearchFailureLogFileName))
                        .CreateLogger(),
                });
        }

        private static LoggingOptions SetDefault(LoggingOptions options)
        {
            options ??= new LoggingOptions
            {
            };

            options.LogLevel ??= new Dictionary<string, string>();

            // codequality-team 2026-10-20 run-1625 Agent3-BestPractices:
            // TryAdd 1 lần thay vì ContainsKey + indexer (CA1854): 1 lookup
            // thay vì 2, nguyên tử, semantics giữ nguyên (chỉ thêm khi vắng).
            options.LogLevel.TryAdd("Default", "Warning");

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
            // context có thể null khi log event không mang property
            // "SourceContext" (call site truyền null ở nhánh TryGetValue false) —
            // dereference trực tiếp sẽ ném NullReferenceException ngay trong
            // pipeline của Serilog. Coalesce về chuỗi rỗng: không key nào khớp
            // (StartsWith trên chuỗi rỗng luôn false với key khác rỗng) nên
            // rơi về level "Default" như thiết kế.
            context = (context ?? string.Empty).Replace("\"", string.Empty);
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
                ApplySerilogConfiguration(logging, context.Configuration, context.HostingEnvironment, logOptions));

            return builder;
        }

        public static IHostBuilder UsePhuThuongStickyRiceLogger(this IHostBuilder builder, Func<IConfiguration, LoggingOptions> logOptions)
        {
            builder.ConfigureLogging((context, logging) =>
                ApplySerilogConfiguration(logging, context.Configuration, context.HostingEnvironment, logOptions));

            return builder;
        }

        /// <summary>
        /// Cấu hình Serilog dùng chung cho cả WebHost (context.HostingEnvironment là
        /// <see cref="IWebHostEnvironment"/>) và Host thường — tránh trùng lặp vì 2 loại
        /// context đều lộ <see cref="IConfiguration"/> + <see cref="IHostEnvironment"/>.
        /// </summary>
        private static void ApplySerilogConfiguration(
            ILoggingBuilder logging,
            IConfiguration configuration,
            IHostEnvironment hostingEnvironment,
            Func<IConfiguration, LoggingOptions> logOptions)
        {
            logging.Configure(options =>
            {
                // options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
            });

            logging.AddAzureWebAppDiagnostics();

            logging.AddSerilog();

            LoggingOptions options = SetDefault(logOptions(configuration));

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

            hostingEnvironment.UsePhuThuongStickyRiceLogger(options);
        }

        private static void UsePhuThuongStickyRiceLogger(
            this IHostEnvironment env, LoggingOptions options, Action<LoggerConfiguration> enrich = null)
        {
            var loggerConfiguration = CreateBaseLoggerConfiguration(
                env.ContentRootPath, env.ApplicationName, env.EnvironmentName);

            enrich?.Invoke(loggerConfiguration);

            ApplyFileAndElasticsearchSinks(loggerConfiguration, env.ContentRootPath, options);

            Log.Logger = loggerConfiguration.CreateLogger();
        }
    }
}
