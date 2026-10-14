using PhuThuongStickyRice.Application.Wave4;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Logging hook for the system log (item 104): Warning+ entries are
    /// persisted to the AppLogs table (capped at the latest 2000 rows).
    /// Self-categories (AppLog, Webhook, SystemHealth) are skipped to avoid
    /// infinite recursion. Persistence goes through a fresh DI scope so it
    /// works even when the log originates outside a request scope.
    /// </summary>
    public class AppLogLoggerProvider : ILoggerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, AppLogLogger> _loggers = new ConcurrentDictionary<string, AppLogLogger>();

        public AppLogLoggerProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new AppLogLogger(name, _serviceProvider));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }

        private sealed class AppLogLogger : ILogger
        {
            private readonly string _category;
            private readonly IServiceProvider _serviceProvider;

            public AppLogLogger(string category, IServiceProvider serviceProvider)
            {
                _category = category;
                _serviceProvider = serviceProvider;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= LogLevel.Warning;
            }

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state,
                Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                // Never persist logs about the log pipeline itself.
                if (_category.IndexOf("AppLog", StringComparison.OrdinalIgnoreCase) >= 0
                    || _category.IndexOf("Webhook", StringComparison.OrdinalIgnoreCase) >= 0
                    || _category.IndexOf("SystemHealth", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }

                string message;
                try
                {
                    message = formatter != null ? formatter(state, exception) : state?.ToString();
                }
                catch
                {
                    message = state?.ToString();
                }

                if (exception != null)
                {
                    message = (message ?? string.Empty) + " | " + exception.GetType().Name + ": " + exception.Message;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                // Fire-and-forget on purpose: logging must never slow the request.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var writer = scope.ServiceProvider.GetService<IAppLogWriter>();
                        if (writer != null)
                        {
                            await writer.WriteAsync(logLevel.ToString(), _category, message);
                        }
                    }
                    catch
                    {
                        // The log hook itself must never throw.
                    }
                });
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new NullScope();

                public void Dispose()
                {
                }
            }
        }
    }
}
