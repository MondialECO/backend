using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface INotificationService
    {
        Task<IEnumerable<Notifications>> GetByUser(string userId);
        Task MarkRead(string notificationId);
    }
}
