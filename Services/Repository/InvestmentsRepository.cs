using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class InvestmentsRepository : MongoRepository<Investments>
    {
        public InvestmentsRepository(IMongoDatabase database) : base(database, "Investments") 
        {
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
        private async Task CreateIndexesAsync()
        {
            // Index on InvestorId for fast queries
            var investorIndex = Builders<Investments>.IndexKeys.Ascending(i => i.InvestorId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<Investments>(investorIndex));

            // Index on IdeaId for fast queries
            var ideaIndex = Builders<Investments>.IndexKeys.Ascending(i => i.IdeaId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<Investments>(ideaIndex));
        }

        public async Task<IEnumerable<Investments>> GetByInvestorIdAsync(string investorId)
        {
            var filter = Builders<Investments>.Filter.Eq(i => i.InvestorId, investorId);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<Investments>> GetByIdeaIdAsync(string ideaId)
        {
            var filter = Builders<Investments>.Filter.Eq(i => i.IdeaId, ideaId);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<Investments>> GetByIdeaIdsAsync(List<string> ideaIds)
        {
            var filter = Builders<Investments>.Filter.In(i => i.IdeaId, ideaIds);
            return await _collection.Find(filter).ToListAsync();
        }

    }
}
