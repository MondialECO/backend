
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
            var filter = Builders<BusinessIdeas>.Filter.Eq(i => i.Id, id);

            var update = Builders<BusinessIdeas>.Update
                .Set(x => x.Title, idea.Title)
                .Set(x => x.Summary, idea.Summary)
                .Set(x => x.MarketSize, idea.MarketSize)
                .Set(x => x.Problem, idea.Problem)
                .Set(x => x.Solution, idea.Solution)
                .Set(x => x.RevenueModel, idea.RevenueModel)
                .Set(x => x.Stage, idea.Stage)
                .Set(x => x.FundingRequired, idea.FundingRequired)
                .Set(x => x.EquityOffered, idea.EquityOffered)
                .Set(x => x.Milestones, idea.Milestones)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update);
        }






    }
}
