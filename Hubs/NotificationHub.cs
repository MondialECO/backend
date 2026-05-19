
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using WebApp.Hubs;
using WebApp.Services;
using WebApp.Services.Interface;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IPresenceTracker _presence;

    public NotificationHub(IPresenceTracker presence) => _presence = presence;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _presence.UserConnectedAsync(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await _presence.UserDisconnectedAsync(userId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

