using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class Conversations
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? projectId { get; set; } // Reference to the associated project
        public string participants { get; set; } // Comma-separated user IDs of participants
        public DateTime updatedAt { get; set; } // Timestamp of the last update
    }
}
