using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Services
{
    public class PushSubscriptionEntityService : IPushSubscriptionEntity
    {
        private readonly PushSubscriptionEntityRepository _repo;
        public PushSubscriptionEntityService(PushSubscriptionEntityRepository repo) 
        { 
            _repo = repo;
        }

        public async Task<List<PushSubscriptionEntity>> GetByUserId(Guid userId)
        {
            return await _repo.GetByUserId(userId);
        }

        public async Task AddSubscription(PushSubscriptionEntity subscription)
        {
            await _repo.AddSubscription(subscription);
        }
        public async Task DeactivateSubscription(Guid userId, string endpoint)
        {
            await _repo.DeactivateSubscription(userId, endpoint);
        }
        public async Task DeactivateAllSubscriptions(Guid userId)
        {
            await _repo.DeactivateAllSubscriptions(userId);
        }
        public async Task ActivateSubscription(Guid userId, string endpoint)
        {
            await _repo.ActivateSubscription(userId, endpoint);
        }
        public async Task DeleteSubscription(Guid userId, string endpoint)
        {
            await _repo.DeleteSubscription(userId, endpoint);
        }


    }
}
