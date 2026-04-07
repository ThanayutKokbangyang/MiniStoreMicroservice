using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Common.Interfaces.Infrastructure;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Interfaces.Services;
using Shared.Infrastructure.Caching;
using Shared.Infrastructure.Data;
using Shared.Infrastructure.Data.Repositories;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Extensions
{
    // ============================================================
    // Service Registration Extensions
    // Design Pattern: Builder Pattern
    // ============================================================
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register shared infrastructure services (DB, Auth, Logging, Cache)
        /// </summary>

        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services,IConfiguration configuration)
        {
            // Database
            services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
            services.AddTransient<DatabaseInitializer>();

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            // Security
            services.AddScoped<ITokenService, TokenService>();

            // Caching
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();

            // Audit Logging
            services.AddHttpContextAccessor();
            services.AddScoped<IAuditLogService, AuditLogService>();

            // JWT Authentication
            services.AddJwtAuthentication(configuration);

            return services;
        }

        /// <summary>
        /// Configure JWT Authentication
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true; // บังคับ HTTPS
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // ไม่อนุญาต token ที่หมดอายุแม้แต่วินาทีเดียว
                    RequireExpirationTime = true
                };

                // Event handlers สำหรับ logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // ป้องกัน default behavior ที่ redirect ไป login page
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(
                            """{"success":false,"message":"Authentication required","errors":["Invalid or missing token"]}""");
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
            });

            return services;
        }
    }
}
