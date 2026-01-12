using MongoDB.Bson;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface INotificationService
    {
        Task<Notification> CreateNotification(Guid userId, string title, string body);
        Task NotifyUser(Guid userId, string title, string body); // offline push
        Task<List<Notification>> GetUserNotifications(Guid userId, int skip = 0, int limit = 30);
        Task MarkAsRead(ObjectId notificationId);
    }
}
