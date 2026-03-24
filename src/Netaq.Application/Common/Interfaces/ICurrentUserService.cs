namespace Netaq.Application.Common.Interfaces;

/// <summary>
/// Provides current authenticated user context.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    string? Email { get; }
    string? Role { get; }
    string? Locale { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
}
