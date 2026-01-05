using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class ChatMessage
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("ConversationId")]
        public ObjectId ConversationId { get; set; }

        [BsonElement("SenderId")]
        public Guid SenderId { get; set; }

        [BsonElement("Message")]
        public string Message { get; set; } = "";

        [BsonElement("MessageType")]
        public string MessageType { get; set; } = "Text"; // Text | Image | File

        [BsonElement("IsRead")]
        public bool IsRead { get; set; } = false;

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
