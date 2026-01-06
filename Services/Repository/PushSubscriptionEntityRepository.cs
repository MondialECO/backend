using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class PushSubscriptionEntityRepository : MongoRepository<PushSubscriptionEntity>
    {
        public PushSubscriptionEntityRepository(IMongoDatabase database) : base(database, "PushSubscriptions")
        {
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
        private async Task CreateIndexesAsync()
        {
            var index = Builders<PushSubscriptionEntity>.IndexKeys.Ascending(n => n.UserId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<PushSubscriptionEntity>(index));
        }

        public async Task<List<PushSubscriptionEntity>> GetByUserId(Guid userId)
        {
            return await _collection.Find(n => n.UserId == userId && n.IsActive).ToListAsync();
        }

        public async Task AddSubscription(PushSubscriptionEntity subscription)
        {
            await _collection.InsertOneAsync(subscription);
        }

        public async Task DeactivateSubscription(Guid userId, string endpoint)
        {
            await _collection.UpdateOneAsync(
                n => n.UserId == userId && n.Endpoint == endpoint,
                Builders<PushSubscriptionEntity>.Update.Set(n => n.IsActive, false)
            );
        }

        public async Task DeactivateAllSubscriptions(Guid userId)
        {
            await _collection.UpdateManyAsync(
                n => n.UserId == userId,
                Builders<PushSubscriptionEntity>.Update.Set(n => n.IsActive, false)
            );
        }

        public async Task ActivateSubscription(Guid userId, string endpoint)
        {
            await _collection.UpdateOneAsync(
                n => n.UserId == userId && n.Endpoint == endpoint,
                Builders<PushSubscriptionEntity>.Update.Set(n => n.IsActive, true)
            );
        }

        public async Task DeleteSubscription(Guid userId, string endpoint)
        {
            await _collection.DeleteOneAsync(
                n => n.UserId == userId && n.Endpoint == endpoint
            );
        }

    }
}
