using WebApp.DbContext;
using WebApp.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Repository
{
    public class UserGeolocationRepository : IUserGeolocationRepository
    {
        private readonly IMongoCollection<GeolocationModel> _userGeolocation;

        public UserGeolocationRepository(MongoDbContext context)
        {
            _userGeolocation = context.UserGeolocation;
        }

        public async Task<GeolocationModel> GetByIpAddressAsync(string ipAddress)
        {
            var filter = Builders<GeolocationModel>.Filter.Eq("IpAddress", ipAddress);
            var sort = Builders<GeolocationModel>.Sort.Descending(g => g.CreatedAt);
            return await _userGeolocation.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }

        public async Task<GeolocationModel> GetByUserName(string userName)
        {
            var filter = Builders<GeolocationModel>.Filter.Eq(g => g.UserName, userName);
            var sort = Builders<GeolocationModel>.Sort.Descending(g => g.CreatedAt);
            return await _userGeolocation.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }

        public async Task<GeolocationModel> GetByUserNameAndIpAddressAsync(string userName, string ipAddress)
        {
            var filter = Builders<GeolocationModel>.Filter.And(
                Builders<GeolocationModel>.Filter.Eq(g => g.UserName, userName),
                Builders<GeolocationModel>.Filter.Eq(g => g.IpAddress, ipAddress)
            );
            var sort = Builders<GeolocationModel>.Sort.Descending(g => g.CreatedAt);

            return await _userGeolocation.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }



        public async Task AddAsync(GeolocationModel geolocation)
        {
            await _userGeolocation.InsertOneAsync(geolocation);
        }

        public async Task UpdateAsync(GeolocationModel geolocation)
        {
            var filter = Builders<GeolocationModel>.Filter.Eq("_id", geolocation.Id);
            await _userGeolocation.ReplaceOneAsync(filter, geolocation);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<GeolocationModel>.Filter.Eq("_id", new ObjectId(id));
            await _userGeolocation.DeleteOneAsync(filter);
        }

        public async Task<List<GeolocationModel>> GetGeolocationsNearby(double loc, double maxDistanceInMeters)
        {
            // Example implementation for geolocation proximity search (optional)
            var filter = Builders<GeolocationModel>.Filter.NearSphere(
                g => g.Loc,
                loc,
                maxDistanceInMeters);

            return await _userGeolocation.Find(filter).ToListAsync();
        }
        public async Task<GeolocationModel> GetByIdAsync(string Id)
        {
            if (!Guid.TryParse(Id, out var guidId))
            {
                throw new ArgumentException("Invalid ID format. Must be a valid Guid.", nameof(Id));
            }

            var filter = Builders<GeolocationModel>.Filter.Eq(g => g.Id, guidId);
            return await _userGeolocation.Find(filter).FirstOrDefaultAsync();
        }

    }
}
