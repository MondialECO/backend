using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class Messages
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? conversationId { get; set; } // Reference to the associated conversation
        public string senderId { get; set; } // User ID of the message sender
        public string message { get; set; } // Content of the message
        public bool isRead { get; set; } // Read status of the message
        public DateTime createdAt { get; set; } // Timestamp of message creation
    }
}
