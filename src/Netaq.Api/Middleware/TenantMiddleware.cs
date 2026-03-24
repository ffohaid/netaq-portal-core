using System.Security.Claims;
using Netaq.Infrastructure.Persistence.Interceptors;

namespace Netaq.Api.Middleware;

/// <summary>
/// Middleware to extract tenant (organization) ID from JWT claims
/// and set it in the TenantProvider for RLS filtering.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var orgIdClaim = context.User.FindFirstValue("organizationId");
            if (Guid.TryParse(orgIdClaim, out var orgId))
            {
                tenantProvider.SetCurrentTenantId(orgId);
            }
        }

        await _next(context);
    }
}
