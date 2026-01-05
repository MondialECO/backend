
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using WebApp.Hubs;
using WebApp.Services.Interface;

[Authorize]
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;
    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            PresenceTracker.UserConnected(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            PresenceTracker.UserDisconnected(userId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendNotification(Guid userId, string title, string body)
    {
        var notification = await _notificationService.CreateNotification(userId, title, body);

        if (PresenceTracker.IsOnline(userId.ToString()))
        {
            // Real-time push
            await Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notification);
        }
        else
        {
            // Offline: push via FCM / OneSignal / Web Push
            await _notificationService.SendPushNotification(userId, notification);
        }
    }
}

