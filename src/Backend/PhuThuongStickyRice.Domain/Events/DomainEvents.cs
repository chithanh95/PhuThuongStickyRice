using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Events
{
    /// <summary>
    /// Bus sự kiện domain nội bộ: entity phát sự kiện (ví dụ đơn tạo xong)
    /// và các handler đã đăng ký được gọi bất đồng bộ. Handler đăng ký 1 lần
    /// lúc khởi động qua <see cref="RegisterHandlers"/>.
    /// </summary>
    public class DomainEvents : IDomainEvents
    {
        private static List<Type> _handlers = new List<Type>();
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Quét assembly tìm mọi class implement <see cref="IDomainEventHandler{T}"/>
        /// và đăng ký transient vào DI. Danh sách handler lưu static để
        /// <see cref="DispatchAsync"/> biết gọi ai.
        /// </summary>
        public static void RegisterHandlers(Assembly assembly, IServiceCollection services)
        {
            var types = assembly.GetTypes()
                                .Where(x => x.GetInterfaces().Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
                                .ToList();

            foreach (var type in types)
            {
                services.AddTransient(type);
            }

            _handlers.AddRange(types);
        }

        public DomainEvents(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Phát sự kiện tới mọi handler đã đăng ký mà xử lý đúng kiểu sự
        /// kiện này. Mỗi handler được resolve từ DI và chạy
        /// <see cref="IDomainEventHandler{T}.HandleAsync"/>.
        /// </summary>
        public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            foreach (Type handlerType in _handlers)
            {
                bool canHandleEvent = handlerType.GetInterfaces()
                    .Any(x => x.IsGenericType
                        && x.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
                        && x.GenericTypeArguments[0] == domainEvent.GetType());

                if (canHandleEvent)
                {
                    dynamic handler = _serviceProvider.GetService(handlerType);
                    await handler.HandleAsync((dynamic)domainEvent, cancellationToken);
                }
            }
        }
    }
}
