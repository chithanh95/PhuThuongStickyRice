using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentity(this IServiceCollection services)
        {
            services.AddIdentity<User, Role>()
                    .AddTokenProviders()
                    .AddPasswordValidators()
                    // Lỗi Identity sang tiếng Việt (2026-10-23): mặc định
                    // IdentityErrorDescriber trả tiếng Anh ra form khách.
                    .AddErrorDescriber<VietnameseIdentityErrorDescriber>();

            services.AddTransient<IUserStore<User>, UserStore>();
            services.AddTransient<IRoleStore<Role>, RoleStore>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            ConfigureOptions(services);

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
            });

            return services;
        }

        public static IServiceCollection AddIdentityCore(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentityCore<User>()
                    .AddTokenProviders()
                    .AddPasswordValidators()
                    // Lỗi Identity sang tiếng Việt (2026-10-23): mặc định
                    // IdentityErrorDescriber trả tiếng Anh ra form khách.
                    .AddErrorDescriber<VietnameseIdentityErrorDescriber>();

            services.AddTransient<IUserStore<User>, UserStore>();
            services.AddTransient<IRoleStore<Role>, RoleStore>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            ConfigureOptions(services, configuration);

            return services;
        }

        private static IdentityBuilder AddTokenProviders(this IdentityBuilder identityBuilder)
        {
            identityBuilder
                .AddDefaultTokenProviders()
                .AddTokenProvider<EmailConfirmationTokenProvider<User>>("EmailConfirmation");

            return identityBuilder;
        }

        private static IdentityBuilder AddPasswordValidators(this IdentityBuilder identityBuilder)
        {
            identityBuilder
                .AddPasswordValidator<WeakPasswordValidator>()
                .AddPasswordValidator<HistoricalPasswordValidator>();

            return identityBuilder;
        }

        private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration = null)
        {
            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(3);
            });

            services.Configure<EmailConfirmationTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(2);
            });

            // Item 100: lockout policy from appsettings ("Auth" section).
            var maxAttempts = configuration?.GetValue<int?>("Auth:MaxLoginAttempts") ?? 5;
            var lockoutMinutes = configuration?.GetValue<int?>("Auth:LockoutMinutes") ?? 15;

            services.Configure<IdentityOptions>(options =>
            {
                options.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";

                // Default Lockout settings (item 100: configurable).
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                    lockoutMinutes < 1 ? 15 : lockoutMinutes);
                options.Lockout.MaxFailedAccessAttempts = maxAttempts < 1 ? 5 : maxAttempts;
                options.Lockout.AllowedForNewUsers = true;
            });

            services.Configure<PasswordHasherOptions>(option =>
            {
                // option.IterationCount = 10000;
                // option.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2;
            });
        }
    }
}
