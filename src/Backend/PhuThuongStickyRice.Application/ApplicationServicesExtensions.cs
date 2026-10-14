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

            var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();

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
                   .Where(Utils.IsHandlerInterface)
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

            var aggregateRootTypes = typeof(IAggregateRoot).Assembly.GetTypes().Where(x => x.BaseType == typeof(Entity<Guid>) && x.GetInterfaces().Contains(typeof(IAggregateRoot))).ToList();

            var genericHandlerTypes = new[]
            {
                typeof(GetEntititesQueryHandler<>),
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

            return services;
        }
    }
}
