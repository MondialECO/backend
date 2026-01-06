using MongoDB.Bson;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationRepository _repo;
        public readonly NotificationHub _notificationHub;

        public NotificationService(NotificationRepository repo, NotificationHub notificationHub)
        {
            _repo = repo;
            _notificationHub = notificationHub;
        }


        public async Task<Notification> CreateNotification(Guid userId, string title, string body)
        {
            var notif = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body
            };
            await _repo.AddNotification(notif);
            return notif;
        }

        public async Task SendPushNotification(Guid userId, Notification notification)
        {
            // implement FCM / Web Push here
            //await _pushService.Send(userId, notification.Title, notification.Body);
            // notificationHub is used here to send real-time notifications as well
            // write business logic to decide whether to use real-time or offline push
            await _notificationHub.SendNotification(userId, notification.Title, notification.Body);
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
