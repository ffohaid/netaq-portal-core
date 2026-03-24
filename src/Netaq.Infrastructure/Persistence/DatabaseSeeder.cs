using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;

namespace Netaq.Infrastructure.Persistence;

/// <summary>
/// Seeds initial system data: default organization and system admin account.
/// Only runs on first startup when database is empty.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Only seed if no organizations exist
        if (await context.Organizations.IgnoreQueryFilters().AnyAsync())
            return;

        var orgId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        // Create default organization
        var organization = new Organization
        {
            Id = orgId,
            NameAr = "الجهة الحكومية الافتراضية",
            NameEn = "Default Government Entity",
            DescriptionAr = "الجهة الحكومية الافتراضية لمنصة نِطاق",
            DescriptionEn = "Default government entity for NETAQ Portal",
            Email = "admin@netaq.pro",
            ActiveAuthProvider = AuthProviderType.CustomAuth,
            IsOtpEnabled = true,
            ShowPlatformLogo = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Organizations.Add(organization);

        // Create system admin user
        var (passwordHash, passwordSalt) = HashPassword("NetaqAdmin@2026!");
        
        var admin = new User
        {
            Id = adminId,
            OrganizationId = orgId,
            FullNameAr = "مدير النظام",
            FullNameEn = "System Administrator",
            Email = "admin@netaq.pro",
            Role = OrganizationRole.SystemAdmin,
            Status = UserStatus.Active,
            Locale = "ar",
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);

        // Create default permission matrix for system admin (full access)
        foreach (TenderPhase phase in Enum.GetValues<TenderPhase>())
        {
            context.PermissionMatrices.Add(new PermissionMatrix
            {
                OrganizationId = orgId,
                UserId = adminId,
                TenderPhase = phase,
                UserRole = OrganizationRole.SystemAdmin,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                CanReject = true,
                CanDelegate = true,
                CanExport = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        using var hmac = new HMACSHA512(saltBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hash = Convert.ToBase64String(hashBytes);

        return (hash, salt);
    }
}
