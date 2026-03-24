using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Netaq.Api.Hubs;
using Netaq.Api.Middleware;
using Netaq.Api.Services;
using Netaq.Api.Workers;
using Netaq.Application;
using Netaq.Application.Common.Interfaces;
using Netaq.Infrastructure;
using Netaq.Infrastructure.Ai;
using Microsoft.EntityFrameworkCore;
using Netaq.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration from Environment Variables =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Server=sqlserver;Database=NetaqDb;User Id=sa;Password=NetaqP@ss2026!;TrustServerCertificate=True;";

var redisConnection = builder.Configuration["Redis:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
    ?? "redis:6379,password=NetaqRedis2026!";

var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? "NetaqJwtSecretKey2026!SuperSecureKeyThatIsAtLeast32Characters";

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "netaq.pro";

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "netaq.pro";

var encryptionKey = builder.Configuration["Encryption:Key"]
    ?? Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
    ?? "NetaqAES256EncryptionKey2026!Secure";

var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.hostinger.com";
var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "465");
var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? "admin@netaq.pro";
var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
var smtpFromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? "admin@netaq.pro";
var smtpFromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "NETAQ Portal";

// ===== Add Services =====

// Application Layer (MediatR, FluentValidation)
builder.Services.AddApplication();

// Infrastructure Layer (DB, Redis, JWT, Email, Encryption)
builder.Services.AddInfrastructure(
    connectionString, redisConnection,
    jwtSecret, jwtIssuer, jwtAudience,
    encryptionKey,
    smtpHost, smtpPort, smtpUser, smtpPassword, smtpFromEmail, smtpFromName);

// Current User Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// Background Workers
builder.Services.AddHostedService<SlaTrackingWorker>();
builder.Services.AddHostedService<DocumentIndexingBackgroundService>();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // SignalR token from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NETAQ Portal API",
        Version = "v1",
        Description = "Enterprise GovTech platform for RFP drafting, workflow approvals, and AI-assisted evaluations."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://netaq.pro",
                "http://frontend:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ===== Middleware Pipeline =====

// Global Exception Handler
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Swagger (always enabled for internal platform)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NETAQ Portal API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Tenant Middleware (after auth, before controllers)
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
