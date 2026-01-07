
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using WebApp.Hubs;
using WebApp.Services;
using WebApp.Services.Interface;

[Authorize]
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;
    public readonly WebPushService _webPushService;
    private readonly IPushSubscriptionEntity _pushRepo;
    public NotificationHub(INotificationService notificationService, 
        WebPushService webPushService,
        IPushSubscriptionEntity pushRepo)
    {
        _notificationService = notificationService;
        _webPushService = webPushService;
        _pushRepo = pushRepo;
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
            // Offline: Web Push
            var subscriptions = await _pushRepo.GetByUserId(userId);
            foreach (var sub in subscriptions)
            {
                await _webPushService.SendAsync(sub, notification);
            }
           
        }
    }
}

