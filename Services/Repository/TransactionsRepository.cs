using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class TransactionsRepository : MongoRepository<Transactions>
    {
        public TransactionsRepository(IMongoDatabase database) : base(database, "Transactions")
        {
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
        private async Task CreateIndexesAsync()
        {
            // Index on UserId for fast queries
            var userIndex = Builders<Transactions>.IndexKeys.Ascending(t => t.UserId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<Transactions>(userIndex));

            // Index on TransactionDate for sorting/filtering
            var dateIndex = Builders<Transactions>.IndexKeys.Descending(t => t.TransactionDate);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<Transactions>(dateIndex));
        }

        public async Task<IEnumerable<Transactions>> GetByUserIdAsync(string userId)
        {
            var filter = Builders<Transactions>.Filter.Eq(t => t.UserId, userId);
            return await _collection.Find(filter).ToListAsync();
        }

        // Get recent transactions
        public async Task<IEnumerable<Transactions>> GetRecentAsync(int limit = 50)
        {
            return await _collection.Find(_ => true)
                                    .SortByDescending(t => t.TransactionDate)
                                    .Limit(limit)
                                    .ToListAsync();
        }


    }
}
