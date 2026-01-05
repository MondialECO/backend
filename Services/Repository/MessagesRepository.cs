using MongoDB.Bson;
using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class MessagesRepository : MongoRepository<ChatMessage>
    {
        public MessagesRepository(IMongoDatabase database) : base(database, "ChatMessages")
        {
            // You can create indexes here if needed
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
        private async Task CreateIndexesAsync()
        {

            var index = Builders<ChatMessage>.IndexKeys
                .Ascending(m => m.ConversationId)
                .Descending(m => m.CreatedAt);

            await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<ChatMessage>(index));
        }

        public async Task<List<ChatMessage>> GetMessages(
           ObjectId conversationId, int skip, int limit)
        {
            return await _collection
                .Find(m => m.ConversationId == conversationId)
                .SortByDescending(m => m.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task AddMessage(ChatMessage message)
        {
            await _collection.InsertOneAsync(message);
        }

        public async Task MarkAsRead(ObjectId conversationId, Guid userId)
        {
            var update = Builders<ChatMessage>.Update.Set(m => m.IsRead, true);

            await _collection.UpdateManyAsync(
                m => m.ConversationId == conversationId &&
                     m.SenderId != userId &&
                     !m.IsRead,
                update
            );
        }
    }
}
