using PhuThuongStickyRice.Application;
using PhuThuongStickyRice.Application.EmailMessages.Services;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationServicesExtensions
    {
        /// <summary>
        /// Đăng ký service tầng application: domain event handlers, EntityCrudService
        /// generic và EmailMessageService. Tham số interceptor (tùy chọn) bọc
        /// EntityCrudService của từng aggregate root (dùng cho audit/UoW interceptor).
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, Action<Type, Type, ServiceLifetime> configureInterceptor = null)
        {
            DomainEvents.RegisterHandlers(Assembly.GetExecutingAssembly(), services);

            services
                .AddScoped<IDomainEvents, DomainEvents>()
                .AddScoped(typeof(IEntityCrudService<>), typeof(EntityCrudService<>))
                .AddScoped<EmailMessageService>();

            if (configureInterceptor != null)
            {
                var aggregateRootTypes = typeof(IAggregateRoot).Assembly.GetTypes().Where(x => x.BaseType == typeof(Entity<Guid>) && x.GetInterfaces().Contains(typeof(IAggregateRoot))).ToList();
                foreach (var type in aggregateRootTypes)
                {
                    configureInterceptor(typeof(IEntityCrudService<>).MakeGenericType(type), typeof(EntityCrudService<>).MakeGenericType(type), ServiceLifetime.Scoped);
                }
            }

            return services;
        }

        /// <summary>
        /// Đăng ký hệ CQRS thủ công: dispatcher + toàn bộ command/query handler
        /// (handler cụ thể và handler generic cho mọi aggregate root).
        /// </summary>
        public static IServiceCollection AddMessageHandlers(this IServiceCollection services)
        {
            services.AddScoped<CommandQueryDispatcher>();

            RegisterConcreteHandlers(services, Assembly.GetExecutingAssembly().GetTypes());
            RegisterGenericEntityHandlers(services);

            return services;
        }

        /// <summary>
        /// Đăng ký các handler cụ thể (không generic) trong assembly: mỗi type
        /// triển khai interface handler thì được đăng ký transient qua factory.
        /// </summary>
        /// <param name="services">DI container.</param>
        /// <param name="assemblyTypes">Các type của assembly hiện tại.</param>
        private static void RegisterConcreteHandlers(IServiceCollection services, Type[] assemblyTypes)
        {
            foreach (var type in assemblyTypes)
            {
                // Skip open-generic handler definitions (e.g. AddEntityCommandHandler<T>).
                // MS DI tolerates registering their open-generic interfaces with factories,
                // but Autofac rejects it. The closed versions are registered by the loop below.
                if (type.IsGenericTypeDefinition)
                {
                    continue;
                }

                // best practice: Any() short-circuit thay vì Where().ToList()
                // rồi Count == 0 — scan assembly có hàng trăm type, phần lớn
                // không phải handler: tránh cấp phát List cho mỗi type chỉ để
                // đếm, vẫn giữ nguyên thứ tự đăng ký các interface handler.
                var handlerInterfaces = type.GetInterfaces()
                    .Where(HandlerTypeInspector.IsHandlerInterface);
                if (!handlerInterfaces.Any())
                {
                    continue;
                }

                var handlerFactory = new HandlerFactory(type);
                foreach (var interfaceType in handlerInterfaces)
                {
                    services.AddTransient(interfaceType, provider => handlerFactory.Create(provider, interfaceType));
                }
            }
        }

        /// <summary>
        /// Đăng ký các handler generic cho mọi aggregate root
        /// (Get/AddOrUpdate/Delete entity) bằng cách đóng generic từng entity.
        /// </summary>
        /// <param name="services">DI container.</param>
        private static void RegisterGenericEntityHandlers(IServiceCollection services)
        {
            var aggregateRootTypes = typeof(IAggregateRoot).Assembly.GetTypes().Where(x => x.BaseType == typeof(Entity<Guid>) && x.GetInterfaces().Contains(typeof(IAggregateRoot))).ToList();

            var genericHandlerTypes = new[]
            {
                typeof(GetEntitiesQueryHandler<>),
                typeof(GetEntityByIdQueryHandler<>),
                typeof(AddOrUpdateEntityCommandHandler<>),
                typeof(DeleteEntityCommandHandler<>),
            };

            foreach (var aggregateRootType in aggregateRootTypes)
            {
                foreach (var genericHandlerType in genericHandlerTypes)
                {
                    var handlerType = genericHandlerType.MakeGenericType(aggregateRootType);
                    var handlerInterfaces = handlerType.GetInterfaces();

                    var handlerFactory = new HandlerFactory(handlerType);
                    foreach (var interfaceType in handlerInterfaces)
                    {
                        services.AddTransient(interfaceType, provider => handlerFactory.Create(provider, interfaceType));
                    }
                }
            }
        }
    }
}
