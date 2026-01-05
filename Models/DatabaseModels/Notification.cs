using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class Notification
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public Guid UserId { get; set; }

        [BsonElement("Title")]
        public string Title { get; set; } = "";

        [BsonElement("Body")]
        public string Body { get; set; } = "";

        [BsonElement("Type")]
        public string Type { get; set; } = "";
        // Message | Investment | System | Security

        [BsonElement("ReferenceId")]
        public ObjectId? ReferenceId { get; set; }
        // ConversationId / ProjectId / InvestmentId

        [BsonElement("IsRead")]
        public bool IsRead { get; set; } = false;

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
