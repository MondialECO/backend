using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface IPushSubscriptionEntity
    {
        Task AddSubscription(PushSubscriptionEntity subscription);
        Task DeactivateSubscription(Guid userId, string endpoint);
        Task DeactivateAllSubscriptions(Guid userId);
        Task ActivateSubscription(Guid userId, string endpoint);
        Task DeleteSubscription(Guid userId, string endpoint);
        Task <List<PushSubscriptionEntity>> GetByUserId(Guid userId);
    }
}
