using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Netaq.Application.Ai.Commands;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Ai;
using Netaq.Infrastructure.Cache;
using Netaq.Infrastructure.Encryption;
using Netaq.Infrastructure.Export;
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
        
        // HTTP Client Factory (required for AI and RAG services)
        services.AddHttpClient();
        
        // Sprint 2: AI Drafting Service (database-driven provider selection)
        // Register base service for direct use and DI resolution
        services.AddScoped<DraftingAiService>();
        
        // Sprint 2: RAG Service (Qdrant vector database integration)
        services.AddScoped<IRagService, RagService>();
        
        // Sprint 2: RAG-Enhanced AI Drafting Service (wraps base with knowledge base context)
        // This is the primary IAiDraftingService implementation
        services.AddScoped<IAiDraftingService, RagEnhancedDraftingService>();
        
        // Sprint 2: Document Export Service
        services.AddScoped<IDocumentExportService, DocumentExportService>();
        
        return services;
    }
}
