using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class PushSubscriptionEntity
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public Guid UserId { get; set; }

        [BsonElement("Endpoint")]
        public string Endpoint { get; set; } = null!;

        [BsonElement("P256dh")]
        public string P256dh { get; set; } = null!;

        [BsonElement("Auth")]
        public string Auth { get; set; } = null!;

        [BsonElement("UserAgent")]
        public string? UserAgent { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
