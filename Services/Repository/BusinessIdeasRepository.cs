
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Models.DatabaseModels;

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
            // 1️ Founder dashboard (very frequent)
            await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BusinessIdeas>(
                    Builders<BusinessIdeas>.IndexKeys.Ascending(x => x.CreatorId)
                )
            );

            // 2️ Admin moderation queue
            await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BusinessIdeas>(
                    Builders<BusinessIdeas>.IndexKeys
                        .Ascending(x => x.Status)
                        .Ascending(x => x.CreatedAt)
                )
            );

            // 3️ Investor discovery (MOST IMPORTANT)
            await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BusinessIdeas>(
                    Builders<BusinessIdeas>.IndexKeys
                        .Ascending(x => x.IsPublished)
                        .Ascending(x => x.Status)
                        .Descending(x => x.ReadinessScore),
                    new CreateIndexOptions<BusinessIdeas>   //  IMPORTANT
                    {
                        Name = "Investor_Discovery_Index",
                        PartialFilterExpression =
                            Builders<BusinessIdeas>.Filter.Eq(x => x.Status, "Approved")
                    }
                )
            );


            // 4️ Stage analytics / filtering
            await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BusinessIdeas>(
                    Builders<BusinessIdeas>.IndexKeys.Ascending(x => x.Status)
                )
            );
        }


        // Custom methods specific to BusinessIdeas for founder dashboard and admin moderation
        public async Task<IEnumerable<BusinessIdeas>> GetByCreatorIdAsync(string creatorId)
        {
            var filter = Builders<BusinessIdeas>.Filter.Eq(b => b.CreatorId, creatorId);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<BusinessIdeas> GetByIdeaDriftAsync(string ideaDriftId, string creatorId)
        {
            var filter = Builders<BusinessIdeas>.Filter.And(
                Builders<BusinessIdeas>.Filter.Eq(b => b.CreatorId, creatorId),
                Builders<BusinessIdeas>.Filter.Eq(b => b.Id, ideaDriftId)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<BusinessIdeas>> GetPendingIdeasAsync()
        {
            var filter = Builders<BusinessIdeas>.Filter.Eq(b => b.Status, "Pending");
            return await _collection.Find(filter).ToListAsync();
        }

        //public async Task UpdatePartialAsync(string id, BusinessIdeas idea)
        //{
        //    var filter = Builders<BusinessIdeas>.Filter.Eq(i => i.Id, id);

        //    var update = Builders<BusinessIdeas>.Update
        //        .Set(x => x.Title, idea.Title)
        //        .Set(x => x.Summary, idea.Summary)
        //        .Set(x => x.MarketSize, idea.MarketSize)
        //        .Set(x => x.Problem, idea.Problem)
        //        .Set(x => x.Solution, idea.Solution)
        //        .Set(x => x.RevenueModel, idea.RevenueModel)
        //        .Set(x => x.Stage, idea.Stage)
        //        .Set(x => x.FundingRequired, idea.FundingRequired)
        //        .Set(x => x.EquityOffered, idea.EquityOffered)
        //        .Set(x => x.Milestones, idea.Milestones)
        //        .Set(x => x.UpdatedAt, DateTime.UtcNow);

        //    await _collection.UpdateOneAsync(filter, update);
        //}






    }
}
