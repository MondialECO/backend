using MongoDB.Bson;
using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class ConversationRepository : MongoRepository<Conversation>
    {
        public ConversationRepository(IMongoDatabase database) : base(database, "Conversations")
        {
            // You can create indexes here if needed
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
        private async Task CreateIndexesAsync()
        {
            var index = Builders<Conversation>.IndexKeys.Ascending(c => c.Participants);
            await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<Conversation>(index));
        }

        public async Task<List<Conversation>> GetUserConversations(Guid userId)
        {
            return await _collection
                .Find(c => c.Participants.Contains(userId))
                .SortByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<Conversation> GetOrCreateConversation(Guid user1, Guid user2)
        {
            var convo = await _collection.Find(c =>
                c.Participants.Contains(user1) &&
                c.Participants.Contains(user2) &&
                c.Type == "Direct"
            ).FirstOrDefaultAsync();

            if (convo != null) return convo;

            var newConvo = new Conversation
            {
                Participants = new List<Guid> { user1, user2 }
            };

            await _collection.InsertOneAsync(newConvo);
            return newConvo;
        }

        public async Task UpdateConversionLastMessage(ChatMessage update)
        {
            var filter = Builders<Conversation>.Filter.Eq(c => c.Id, update.ConversationId);
            var updateDef = Builders<Conversation>.Update
                .Set(c => c.LastMessage, update.Message)
                .Set(c => c.LastMessageAt, update.CreatedAt);
            await _collection.UpdateOneAsync(filter, updateDef);
            //await _collection.UpdateOneAsync(c => c.Id == update.ConversationId, updateDef);
        }
       

    }
}
