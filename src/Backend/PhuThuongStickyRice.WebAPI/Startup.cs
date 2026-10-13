using PhuThuongStickyRice.CrossCuttingConcerns.Csv;
using PhuThuongStickyRice.CrossCuttingConcerns.Excel;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Infrastructure.Csv;
using PhuThuongStickyRice.Infrastructure.Excel.ClosedXML;
using PhuThuongStickyRice.Infrastructure.Identity;
using PhuThuongStickyRice.Infrastructure.Localization;
using PhuThuongStickyRice.Infrastructure.Monitoring;
using PhuThuongStickyRice.Infrastructure.Web.Filters;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.Authorization;
using PhuThuongStickyRice.WebAPI.ConfigurationOptions;
using PhuThuongStickyRice.WebAPI.Hubs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PhuThuongStickyRice.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;

            AppSettings = new AppSettings();
            Configuration.Bind(AppSettings);
        }

        public IConfiguration Configuration { get; }
        private AppSettings AppSettings { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration);

            services.AddMonitoringServices(AppSettings.Monitoring);

            services.AddControllers(configure =>
            {
                configure.Filters.Add(typeof(GlobalExceptionFilter));
            })
            .ConfigureApiBehaviorOptions(options =>
            {
            })
            .AddJsonOptions(options =>
            {
            });

            services.AddSignalR();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", builder => builder
                    .WithOrigins(AppSettings.CORS.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader());

                options.AddPolicy("SignalRHubs", builder => builder
                    .WithOrigins(AppSettings.CORS.AllowedOrigins)
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST")
                    .AllowCredentials());

                options.AddPolicy("AllowAnyOrigin", builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());

                options.AddPolicy("CustomPolicy", builder => builder
                    .AllowAnyOrigin()
                    .WithMethods("Get")
                    .WithHeaders("Content-Type"));
            });

            services.AddDateTimeProvider();

            services.AddMultiTenantPersistence(typeof(StickyRiceDbContextMultiTenantConnectionStringResolver), typeof(TenantResolver))
                    .AddDomainServices()
                    .AddApplicationServices((Type serviceType, Type implementationType, ServiceLifetime serviceLifetime) =>
                    {
                        services.AddInterceptors(serviceType, implementationType, serviceLifetime, AppSettings.Interceptors);
                    })
                    .AddMessageHandlers()
                    .ConfigureInterceptors()
                    .AddIdentityCore();

            services.AddDataProtection()
                .PersistKeysToDbContext<StickyRiceDbContext>()
                .SetApplicationName("PhuThuongStickyRice");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    if (AppSettings.IdentityServerAuthentication.Provider == "OpenIddict")
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateAudience = false,
                            ValidIssuer = AppSettings.IdentityServerAuthentication.OpenIddict.IssuerUri,
                            TokenDecryptionKey = new X509SecurityKey(AppSettings.IdentityServerAuthentication.OpenIddict.TokenDecryptionCertificate.FindCertificate()),
                            IssuerSigningKey = new X509SecurityKey(AppSettings.IdentityServerAuthentication.OpenIddict.IssuerSigningCertificate.FindCertificate()),
                        };
                    }
                    else
                    {
                        options.Authority = AppSettings.IdentityServerAuthentication.Authority;
                        options.Audience = AppSettings.IdentityServerAuthentication.ApiName;
                        options.RequireHttpsMetadata = AppSettings.IdentityServerAuthentication.RequireHttpsMetadata;
                    }
                });

            services.AddAuthorizationPolicies(Assembly.GetExecutingAssembly(), AuthorizationPolicyNames.GetPolicyNames());

            services.AddRateLimiter(options =>
            {
                options.AddPolicy<string, DefaultRateLimiterPolicy>(RateLimiterPolicyNames.DefaultPolicy);
                options.AddPolicy<string, GetAuditLogsRateLimiterPolicy>(RateLimiterPolicyNames.GetAuditLogsPolicy);
            });

            services.AddSwaggerGen(setupAction =>
            {
                setupAction.SwaggerDoc(
                    $"PhuThuongStickyRice",
                    new OpenApiInfo()
                    {
                        Title = "PhuThuongStickyRice API",
                        Version = "1",
                        Description = "PhuThuongStickyRice API Specification.",
                        Contact = new OpenApiContact
                        {
                            Email = "abc.xyz@gmail.com",
                            Name = "Phong Nguyen",
                            Url = new Uri("https://github.com/phongnguyend"),
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT License",
                            Url = new Uri("https://opensource.org/licenses/MIT"),
                        },
                    });

                setupAction.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Input your Bearer token to access this API",
                });

                setupAction.AddSecurityDefinition("Oidc", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri(AppSettings.IdentityServerAuthentication.Authority + "/connect/token", UriKind.Absolute),
                            AuthorizationUrl = new Uri(AppSettings.IdentityServerAuthentication.Authority + "/connect/authorize", UriKind.Absolute),
                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "OpenId" },
                                { "profile", "Profile" },
                                { "PhuThuongStickyRice.WebAPI", "PhuThuongStickyRice WebAPI" },
                            },
                        },
                        ClientCredentials = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri(AppSettings.IdentityServerAuthentication.Authority + "/connect/token", UriKind.Absolute),
                            Scopes = new Dictionary<string, string>
                            {
                                { "PhuThuongStickyRice.WebAPI", "PhuThuongStickyRice WebAPI" },
                            },
                        },
                    },
                });

                setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Oidc",
                            },
                        }, new List<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        }, new List<string>()
                    },
                });
            });

            services.AddCaches(AppSettings.Caching);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ICurrentUser, CurrentWebUser>();

            services.AddStorageManager(AppSettings.Storage);
            services.AddHtmlGenerator();
            services.AddDinkToPdfConverter();
            services.AddPhuThuongStickyRiceLocalization();

            services.AddScoped(typeof(ICsvReader<>), typeof(CsvReader<>));
            services.AddScoped(typeof(ICsvWriter<>), typeof(CsvWriter<>));
            services.AddScoped<IExcelReader<List<ConfigurationEntry>>, ConfigurationEntryExcelReader>();
            services.AddScoped<IExcelWriter<List<ConfigurationEntry>>, ConfigurationEntryExcelWriter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDebuggingMiddleware();

            app.UseAccessTokenFromFormMiddleware();

            app.UseLoggingStatusCodeMiddleware();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors(AppSettings.CORS.AllowAnyOrigin ? "AllowAnyOrigin" : "AllowedOrigins");

            app.UseSecurityHeaders(AppSettings.SecurityHeaders);

            app.UseSwagger();

            app.UseSwaggerUI(setupAction =>
            {
                setupAction.OAuthClientId("Swagger");
                setupAction.OAuthClientSecret("secret");
                setupAction.OAuthUsePkce();

                setupAction.SwaggerEndpoint(
                    "/swagger/PhuThuongStickyRice/swagger.json",
                    "PhuThuongStickyRice API");

                setupAction.RoutePrefix = string.Empty;

                setupAction.DefaultModelExpandDepth(2);
                setupAction.DefaultModelRendering(ModelRendering.Model);
                setupAction.DocExpansion(DocExpansion.None);
                setupAction.EnableDeepLinking();
                setupAction.DisplayOperationId();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.UseMonitoringServices(AppSettings.Monitoring);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<NotificationHub>("/hubs/notification").RequireCors("SignalRHubs");
            });
        }
    }
}
