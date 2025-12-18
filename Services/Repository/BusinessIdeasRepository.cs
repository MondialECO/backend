
using WebApp.Models.DatabaseModels;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp.Services.Repository
{
    public class BusinessIdeasRepository : MongoRepository<BusinessIdeas>
    {
        public BusinessIdeasRepository(IMongoDatabase database) : base(database, "BusinessIdeas") 
        {
            // Optional: create indexes for optimization
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
        private async Task CreateIndexesAsync()
        {
            // Index on CreatorId for fast queries
            var creatorIndex = Builders<BusinessIdeas>.IndexKeys.Ascending(b => b.CreatorId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<BusinessIdeas>(creatorIndex));

            // Index on Status for pending/approved filtering
            var statusIndex = Builders<BusinessIdeas>.IndexKeys.Ascending(b => b.Status);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<BusinessIdeas>(statusIndex));
        }

        public async Task<IEnumerable<BusinessIdeas>> GetByCreatorIdAsync(string creatorId)
        {
            var filter = Builders<BusinessIdeas>.Filter.Eq(b => b.CreatorId, creatorId);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<BusinessIdeas>> GetPendingIdeasAsync()
        {
            var filter = Builders<BusinessIdeas>.Filter.Eq(b => b.Status, "Pending");
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task UpdatePartialAsync(string id, BusinessIdeas idea)
        {
            var filter = Builders<BusinessIdeas>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));

            var update = Builders<BusinessIdeas>.Update
                .Set(x => x.Title, idea.Title)
                .Set(x => x.Summary, idea.Summary)
                .Set(x => x.Status, idea.Status)
                .Set(x => x.Stage, idea.Stage);

            await _collection.UpdateOneAsync(filter, update);
        }





    }
}
