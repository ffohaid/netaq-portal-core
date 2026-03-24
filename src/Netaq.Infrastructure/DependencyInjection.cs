using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Cache;
using Netaq.Infrastructure.Encryption;
using Netaq.Infrastructure.Identity;
using Netaq.Infrastructure.Persistence;
using Netaq.Infrastructure.Persistence.Interceptors;
using Netaq.Infrastructure.Services;
using StackExchange.Redis;

namespace Netaq.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string redisConnectionString,
        string jwtSecretKey,
        string jwtIssuer,
        string jwtAudience,
        string encryptionKey,
        string smtpHost,
        int smtpPort,
        string smtpUser,
        string smtpPassword,
        string smtpFromEmail,
        string smtpFromName)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3);
                sqlOptions.CommandTimeout(30);
            }));
        
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        
        // Tenant Provider (scoped per request)
        services.AddScoped<ITenantProvider, TenantProvider>();
        
        // Redis (password-protected, internal Docker network only)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });
        services.AddScoped<ICacheService, RedisCacheService>();
        
        // JWT Token Service
        services.AddSingleton<IJwtTokenService>(new JwtTokenService(jwtSecretKey, jwtIssuer, jwtAudience));
        
        // Encryption Service (AES-256)
        services.AddSingleton<IEncryptionService>(new AesEncryptionService(encryptionKey));
        
        // Email Service
        services.AddSingleton<IEmailService>(new EmailService(
            smtpHost, smtpPort, smtpUser, smtpPassword, smtpFromEmail, smtpFromName));
        
        // Audit Trail Service
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        
        return services;
    }
}
