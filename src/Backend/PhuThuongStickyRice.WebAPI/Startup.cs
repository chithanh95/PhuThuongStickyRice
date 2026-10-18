using PhuThuongStickyRice.CrossCuttingConcerns.Csv;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Infrastructure.Csv;
using PhuThuongStickyRice.Infrastructure.Identity;
using PhuThuongStickyRice.Infrastructure.Monitoring;
using PhuThuongStickyRice.Infrastructure.Web.Filters;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.Application.Orders.Payments;

using PhuThuongStickyRice.Application.Zalo;
using PhuThuongStickyRice.WebAPI.Authorization;
using PhuThuongStickyRice.WebAPI.Services;
using PhuThuongStickyRice.WebAPI.ConfigurationOptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PhuThuongStickyRice.Application.AuditLogs;
using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Application.Zalo;
using PhuThuongStickyRice.Application.Common;

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

            // Item 102: Brotli + Gzip response compression for the API.
            // EnableForHttps = true: the payloads are public catalog/menu
            // JSON (no per-user secrets in compressed responses here).
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
            });

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

            services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", builder => builder
                    .WithOrigins(AppSettings.CORS.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader());

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

            // Self-issued JWT + payment options (bound from appsettings.json).
            // The API issues and validates its own tokens — no external
            // IdentityServer is involved.
            services.Configure<JwtOptions>(Configuration.GetSection("Jwt"));
            services.Configure<MoMoOptions>(Configuration.GetSection("MoMo"));
            services.Configure<BankTransferOptions>(Configuration.GetSection("BankTransfer"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<MoMoOptions>>().Value);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<BankTransferOptions>>().Value);
            services.Configure<ZaloOaOptions>(Configuration.GetSection("ZaloOa"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ZaloOaOptions>>().Value);
            services.AddTransient<IZaloNotificationService, ZaloNotificationService>();
            // Quên mật khẩu bằng OTP SMS cho khách hàng: options + sender
            // pluggable (mặc định chỉ log), OTP lưu IMemoryCache (không migration).
            services.Configure<SmsOptions>(Configuration.GetSection("Sms"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmsOptions>>().Value);
            services.AddMemoryCache();
            services.AddSingleton<ISmsSender, LoggingSmsSender>();
            services.AddScoped<PasswordResetOtpService>();
            services.AddScoped<JwtTokenService>();
            services.AddHttpClient<IMoMoPaymentService, MoMoPaymentService>();
            // Item 96: typed client for reCAPTCHA v3 server verification.
            services.AddHttpClient<IRecaptchaService, RecaptchaService>();
            services.AddHostedService<AdminSeedHostedService>();

            // ABP modules: Application Services (CQRS), EF Core (repositories, DbContext),
            // AutoMapper. Replaces the template AddMultiTenantPersistence (its
            // multi-tenancy was scaffolding that always returned the default
            // connection string).
            services.AddApplication<StickyRiceWebApiModule>();

            services.AddDomainServices()
                    .AddApplicationServices((Type serviceType, Type implementationType, ServiceLifetime serviceLifetime) =>
                    {
                        services.AddInterceptors(serviceType, implementationType, serviceLifetime, AppSettings.Interceptors);
                    })
                    .AddMessageHandlers()
                    .ConfigureInterceptors()
                    .AddIdentityCore(Configuration);

            services.AddDataProtection()
                .PersistKeysToDbContext<StickyRiceDbContext>()
                .SetApplicationName("PhuThuongStickyRice");

            // Self-issued JWT validation: the API validates the tokens it
            // issues itself (see JwtTokenService). No external IdentityServer.
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var jwtOptions = Configuration.GetSection("Jwt").Get<JwtOptions>();
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.Key)),
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                });

            services.AddAuthorizationPolicies(Assembly.GetExecutingAssembly(), AuthorizationPolicyNames.GetPolicyNames());

            services.AddRateLimiter(options =>
            {
                options.AddPolicy<string, DefaultRateLimiterPolicy>(RateLimiterPolicyNames.DefaultPolicy);
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

                setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
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

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ICurrentUser, CurrentWebUser>();

            services.AddHtmlGenerator();
            services.AddDinkToPdfConverter();

            services.AddScoped(typeof(ICsvReader<>), typeof(CsvReader<>));
            services.AddScoped(typeof(ICsvWriter<>), typeof(CsvWriter<>));

            // Item 104: persists Warning+ log entries to the AppLogs table.
            // TEMP DISABLED 2026-10-17: AppLogLoggerProvider causes DbContext
            // concurrency crash (duplicate PK on AppLogs + "second operation"
            // on StickyRiceDbContext). Re-enable after fixing the GUID/
            // concurrency issue in AppLogWriter.
            // services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider, Services.AppLogLoggerProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.InitializeApplication();

            app.UseDebuggingMiddleware();

            // Item 102: must run before UseStaticFiles/UseRouting so API and
            // static responses are compressed.
            app.UseResponseCompression();

            // Serves wwwroot, including /uploads/landing/* for landing page images.
            app.UseStaticFiles();

            // One-click LAN: serve Blazor WASM static files (publish-lan/web/wwwroot)
            // Cấu hình qua appsettings "BlazorWwwRoot" (đường dẫn tương đối từ folder API).
            // Không ảnh hưởng API routes (/api/*) hay Swagger.
            var blazorWwwRoot = Configuration["BlazorWwwRoot"];
            PhysicalFileProvider blazorFileProvider = null;
            if (!string.IsNullOrWhiteSpace(blazorWwwRoot))
            {
                var fullPath = System.IO.Path.IsPathRooted(blazorWwwRoot)
                    ? blazorWwwRoot
                    : System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, blazorWwwRoot));
                if (System.IO.Directory.Exists(fullPath))
                {
                    blazorFileProvider = new PhysicalFileProvider(fullPath);
                    // Blazor WASM cần serve mọi file trong wwwroot (gồm .dat, .wasm, .dll...)
                    // mà StaticFiles mặc định chặn file không rõ MIME type.
                    var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                    contentTypeProvider.Mappings[".dat"] = "application/octet-stream";
                    contentTypeProvider.Mappings[".wasm"] = "application/wasm";
                    contentTypeProvider.Mappings[".dll"] = "application/octet-stream";
                    contentTypeProvider.Mappings[".blat"] = "application/octet-stream";
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = blazorFileProvider,
                        RequestPath = "",
                        ContentTypeProvider = contentTypeProvider,
                        ServeUnknownFileTypes = true
                    });
                    System.Console.WriteLine($"[LAN] Serving Blazor WASM from: {fullPath}");
                }
                else
                {
                    System.Console.WriteLine($"[LAN] BlazorWwwRoot NOT FOUND: {fullPath}");
                }
            }
            else
            {
                System.Console.WriteLine("[LAN] BlazorWwwRoot not configured - API only mode");
            }

            app.UseAccessTokenFromFormMiddleware();

            app.UseLoggingStatusCodeMiddleware();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors(AppSettings.CORS.AllowAnyOrigin ? "AllowAnyOrigin" : "AllowedOrigins");

            app.UseHttpsRedirection();

            app.UseSecurityHeaders(AppSettings.SecurityHeaders);

            app.UseSwagger();

            app.UseSwaggerUI(setupAction =>
            {
                setupAction.SwaggerEndpoint(
                    "/swagger/PhuThuongStickyRice/swagger.json",
                    "PhuThuongStickyRice API");

                // One-click LAN: Blazor chiếm route "/" nên Swagger dời sang "/swagger".
                // Bình thường (không serve Blazor) Swagger vẫn ở root như cũ.
                setupAction.RoutePrefix = blazorFileProvider != null ? "swagger" : string.Empty;

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

                // SPA fallback: mọi route không phải /api/* và không phải file tĩnh
                // thì trả về index.html của Blazor (để client-side routing hoạt động).
                if (blazorFileProvider != null)
                {
                    endpoints.MapFallback(async context =>
                    {
                        var path = context.Request.Path.Value ?? "";
                        if (path.StartsWith("/api/", System.StringComparison.OrdinalIgnoreCase)
                            || path.Equals("/api", System.StringComparison.OrdinalIgnoreCase)
                            || path.StartsWith("/swagger", System.StringComparison.OrdinalIgnoreCase)
                            || System.IO.Path.HasExtension(path))
                        {
                            context.Response.StatusCode = 404;
                            return;
                        }
                        context.Response.ContentType = "text/html";
                        await context.Response.SendFileAsync(blazorFileProvider.GetFileInfo("index.html"));
                    });
                }
            });
        }
    }
}
