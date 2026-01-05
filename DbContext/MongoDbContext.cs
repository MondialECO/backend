using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDbGenericRepository;
using WebApp.Models.DatabaseModels;
namespace WebApp.DbContext
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<ApplicationUser> ApplicationUsers => _database.GetCollection<ApplicationUser>("ApplicationUsers");

        // Business Collections
        public IMongoCollection<BusinessIdeas> BusinessIdeas => _database.GetCollection<BusinessIdeas>("BusinessIdeas");
        public IMongoCollection<Investments> Investments => _database.GetCollection<Investments>("Investments");
        public IMongoCollection<Transactions> Transactions => _database.GetCollection<Transactions>("Transactions");

        // Extra collections
        public IMongoCollection<ContactModel> Contacts => _database.GetCollection<ContactModel>("Contacts");
        public IMongoCollection<FormData> FormDatas => _database.GetCollection<FormData>("FormDatas");

        // Chat Collections
        public IMongoCollection<Conversation> Conversations => _database.GetCollection<Conversation>("Conversations");
        public IMongoCollection<ChatMessage> ChatMessages => _database.GetCollection<ChatMessage>("ChatMessages");
       
        // notifications collection
        public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("Notifications");


    }

}
