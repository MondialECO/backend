using WebApp.DbContext;
using WebApp.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class UserLogsRepository : IUserLogsRepository
    {
        private readonly IMongoCollection<ActivityLogsModel> _userLogs;

        public UserLogsRepository(MongoDbContext context)
        {
            _userLogs = context.UserLogs;
        }

        public async Task<List<ActivityLogsModel>> GetAllAsync()
        {
            var sort = Builders<ActivityLogsModel>.Sort.Descending(log => log.ActivityDate);
            return await _userLogs.Find(_ => true).Sort(sort).ToListAsync();
        }

        public async Task<List<ActivityLogsModel>> GetByIdAsync(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<ActivityLogsModel>.Filter.Eq("_id", objectId);
            var sort = Builders<ActivityLogsModel>.Sort.Descending(log => log.ActivityDate);
            return await _userLogs.Find(filter).Sort(sort).ToListAsync();
        }





        public async Task<ActivityLogsModel> GetByIpAsync(string ipAddress)
        {
            var filter = Builders<ActivityLogsModel>.Filter.Eq("IpAddress", ipAddress);
            var sort = Builders<ActivityLogsModel>.Sort.Descending(g => g.ActivityDate);
            return await _userLogs.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }

        public async Task<ActivityLogsModel> GetByUserNameAsync(string userName)
        {
            var filter = Builders<ActivityLogsModel>.Filter.Eq(g => g.UserName, userName);
            var sort = Builders<ActivityLogsModel>.Sort.Descending(g => g.ActivityDate);
            return await _userLogs.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }

        public async Task<ActivityLogsModel> GetByUserNameAndIpAddressAsync(string userName, string ipAddress)
        {
            var filter = Builders<ActivityLogsModel>.Filter.And(
                Builders<ActivityLogsModel>.Filter.Eq(g => g.UserName, userName),
                Builders<ActivityLogsModel>.Filter.Eq(g => g.IpAddress, ipAddress)
            );
            var sort = Builders<ActivityLogsModel>.Sort.Descending(g => g.ActivityDate);
            return await _userLogs.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }

        public async Task AddAsync(ActivityLogsModel activityLogs)
        {
            await _userLogs.InsertOneAsync(activityLogs);
        }

        public async Task UpdateAsync(ActivityLogsModel activityLogs)
        {
            var filter = Builders<ActivityLogsModel>.Filter.Eq("_id", activityLogs.Id);
            await _userLogs.ReplaceOneAsync(filter, activityLogs);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<ActivityLogsModel>.Filter.Eq("_id", new ObjectId(id));
            await _userLogs.DeleteOneAsync(filter);
        }
    }
}
