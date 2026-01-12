using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using WebApp.Hubs;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationRepository _repo;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IPushSubscriptionEntity _pushRepo;
        public readonly WebPushService _webPushService;


        public NotificationService(NotificationRepository repo,
            IHubContext<NotificationHub> hubContext,
            IPushSubscriptionEntity pushRepo,
            WebPushService webPushService)
        {
            _repo = repo;
            _hubContext = hubContext;
            _pushRepo = pushRepo;
            _webPushService = webPushService;
        }


        public async Task<Notification> CreateNotification(Guid userId, string title, string body)
        {
            var notif = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _repo.AddNotification(notif);
            return notif;
        }


        public async Task NotifyUser(Guid userId, string title, string body)
        {
            var notification = await CreateNotification(userId, title, body);

            if (PresenceTracker.IsOnline(userId.ToString()))
            {
                await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notification);
            }
            else
            {
                var subs = await _pushRepo.GetByUserId(userId);
                foreach (var sub in subs)
                {
                    await _webPushService.SendAsync(sub, notification);
                }
            }
        }





        public async Task<List<Notification>> GetUserNotifications(Guid userId, int skip = 0, int limit = 30)
        {
            return await _repo.GetUserNotifications(userId, skip, limit);
        }

        public async Task MarkAsRead(ObjectId notificationId)
        {
            await _repo.MarkAsRead(notificationId);
        }


    }
}
