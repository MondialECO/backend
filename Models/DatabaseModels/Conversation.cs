using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class Conversation
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Participants")]
        public List<Guid> Participants { get; set; } = new();

        [BsonElement("Type")]
        public string Type { get; set; } = "Direct"; // Direct | Group

        [BsonElement("RelatedProjectId")]
        public ObjectId? RelatedProjectId { get; set; }

        [BsonElement("LastMessage")]
        public string? LastMessage { get; set; }

        [BsonElement("LastMessageAt")]
        public DateTime? LastMessageAt { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
