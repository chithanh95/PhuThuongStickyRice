using PhuThuongStickyRice.Infrastructure.Web.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PolicyServiceCollectionExtensions
    {
        /// <summary>
        /// Đăng ký các authorization policy theo quyền (permission) và các
        /// <see cref="IAuthorizationHandler"/> tương ứng vào DI.
        /// </summary>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services, Assembly assembly, IEnumerable<string> policies)
        {
            services.Configure<AuthorizationOptions>(options =>
            {
                foreach (var policyName in policies)
                {
                    options.AddPolicy(policyName, policy =>
                    {
                        policy.AddRequirements(new PermissionRequirement
                        {
                            PermissionName = policyName
                        });
                    });
                }
            });

            // CA2263: dùng overload generic AddSingleton{TService, TImplementation}()
            // thay cho AddSingleton(typeof, typeof) — cùng hành vi đăng ký.
            services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();

            var requirementHandlerTypes = assembly.GetTypes()
                .Where(IsAuthorizationHandler)
                .ToList();

            foreach (var type in requirementHandlerTypes)
            {
                services.AddSingleton(typeof(IAuthorizationHandler), type);
            }

            return services;
        }

        private static bool IsAuthorizationHandler(Type type)
        {
            if (type.BaseType == null)
            {
                return false;
            }

            if (!type.BaseType.IsGenericType)
            {
                return false;
            }

            var baseType = type.BaseType.GetGenericTypeDefinition();
            return baseType == typeof(AuthorizationHandler<>) || baseType == typeof(AuthorizationHandler<,>);
        }
    }
}
