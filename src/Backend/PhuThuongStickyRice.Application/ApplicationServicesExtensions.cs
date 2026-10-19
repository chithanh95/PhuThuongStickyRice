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
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, Action<Type, Type, ServiceLifetime> configureInterceptor = null)
        {
            DomainEvents.RegisterHandlers(Assembly.GetExecutingAssembly(), services);

            services
                .AddScoped<IDomainEvents, DomainEvents>()
                .AddScoped(typeof(ICrudService<>), typeof(CrudService<>))
                .AddScoped<EmailMessageService>();

            if (configureInterceptor != null)
            {
                var aggregateRootTypes = typeof(IAggregateRoot).Assembly.GetTypes().Where(x => x.BaseType == typeof(Entity<Guid>) && x.GetInterfaces().Contains(typeof(IAggregateRoot))).ToList();
                foreach (var type in aggregateRootTypes)
                {
                    configureInterceptor(typeof(ICrudService<>).MakeGenericType(type), typeof(CrudService<>).MakeGenericType(type), ServiceLifetime.Scoped);
                }
            }

            return services;
        }

        public static IServiceCollection AddMessageHandlers(this IServiceCollection services)
        {
            services.AddScoped<Dispatcher>();

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

                var handlerInterfaces = type.GetInterfaces()
                   .Where(HandlerTypeInspector.IsHandlerInterface)
                   .ToList();

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
