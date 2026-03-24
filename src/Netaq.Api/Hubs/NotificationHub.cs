using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Netaq.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notifications.
/// Sends task assignments, workflow updates, and SLA alerts.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var orgId = Context.User?.FindFirst("organizationId")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Join user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        if (!string.IsNullOrEmpty(orgId))
        {
            // Join organization group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"org_{orgId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var orgId = Context.User?.FindFirst("organizationId")?.Value;

        if (!string.IsNullOrEmpty(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

        if (!string.IsNullOrEmpty(orgId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org_{orgId}");

        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Service for sending notifications via SignalR.
/// </summary>
public interface INotificationService
{
    Task SendToUserAsync(Guid userId, string eventType, object data);
    Task SendToOrganizationAsync(Guid organizationId, string eventType, object data);
}

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(Guid userId, string eventType, object data)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync(eventType, data);
    }

    public async Task SendToOrganizationAsync(Guid organizationId, string eventType, object data)
    {
        await _hubContext.Clients.Group($"org_{organizationId}").SendAsync(eventType, data);
    }
}
