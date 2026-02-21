using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class IdeaClick
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string IdeaId { get; set; }

        public string UserId { get; set; } // optional

        public DateTime ClickedAt { get; set; }
    }
}
