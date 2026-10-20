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
        // Ánh xạ kiểu sự kiện -> các handler xử lý nó, dựng sẵn 1 lần lúc
        // đăng ký để DispatchAsync chỉ tra dictionary O(1) thay vì chạy
        // reflection GetInterfaces() trên mọi handler cho mỗi lần phát sự
        // kiện (trước đây tốn reflection + cấp phát enumerator mỗi dispatch).
        private static readonly Dictionary<Type, List<Type>> _handlersByEvent = new Dictionary<Type, List<Type>>();
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Quét assembly tìm mọi class implement <see cref="IDomainEventHandler{T}"/>
        /// và đăng ký transient vào DI. Đồng thời dựng sẵn ánh xạ kiểu sự kiện
        /// -> handler để <see cref="DispatchAsync"/> biết gọi ai mà không cần
        /// reflection lúc dispatch. Gọi được nhiều lần (nhiều assembly): handler
        /// trùng không bị ghi 2 lần.
        /// </summary>
        public static void RegisterHandlers(Assembly assembly, IServiceCollection services)
        {
            var types = assembly.GetTypes()
                                .Where(type => type.GetInterfaces().Any(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
                                .ToList();

            foreach (var type in types)
            {
                services.AddTransient(type);
            }

            // 1 handler có thể xử lý nhiều loại sự kiện: gom theo từng kiểu sự kiện.
            foreach (var type in types)
            {
                var eventTypes = type.GetInterfaces()
                    .Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                    .Select(iface => iface.GenericTypeArguments[0])
                    .Distinct();

                foreach (var eventType in eventTypes)
                {
                    if (!_handlersByEvent.TryGetValue(eventType, out var handlers))
                    {
                        handlers = new List<Type>();
                        _handlersByEvent[eventType] = handlers;
                    }

                    if (!handlers.Contains(type))
                    {
                        handlers.Add(type);
                    }
                }
            }
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
            // Tra ánh xạ dựng sẵn ở RegisterHandlers: không reflection lúc
            // dispatch, khớp đúng kiểu sự kiện như logic cũ (so sánh == trên
            // GenericTypeArguments).
            if (!_handlersByEvent.TryGetValue(domainEvent.GetType(), out var handlerTypes))
            {
                return;
            }

            foreach (Type handlerType in handlerTypes)
            {
                dynamic handler = _serviceProvider.GetService(handlerType);
                // GetService có thể trả null nếu handler chưa được đăng ký
                // trong DI (ví dụ RegisterHandlers chạy trên service
                // collection khác). Ném lỗi rõ nghĩa thay vì để dynamic
                // dispatch nổ RuntimeBinderException khó đọc.
                if (handler is null)
                    throw new InvalidOperationException(
                        $"Domain event handler '{handlerType.FullName}' chưa được đăng ký trong DI. " +
                        "Kiểm tra RegisterHandlers đã chạy với đúng IServiceCollection.");

                await handler.HandleAsync((dynamic)domainEvent, cancellationToken);
            }
        }
    }
}
