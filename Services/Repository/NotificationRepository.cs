using MongoDB.Bson;
using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class NotificationRepository : MongoRepository<Notification>
    {
        public NotificationRepository(IMongoDatabase database) : base(database, "Notifications")
        {
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
        private async Task CreateIndexesAsync()
        {
            var index = Builders<Notification>.IndexKeys.Ascending(n => n.UserId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(index));
        }

        public async Task AddNotification(Notification notification)
        {
            await _collection.InsertOneAsync(notification);
        }

        public async Task<List<Notification>> GetUserNotifications(Guid userId, int skip, int limit)
        {
            return await _collection
                .Find(n => n.UserId == userId)
                .SortByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task MarkAsRead(ObjectId id)
        {
            await _collection.UpdateOneAsync(
                n => n.Id == id,
                Builders<Notification>.Update.Set(n => n.IsRead, true)
            );
        }
    }
}
