using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class Notifications
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string userId { get; set; } // User ID of the notification recipient
        public string title { get; set; } // Title of the notification
        public string message { get; set; } // Content of the notification
        public string? type { get; set; } // Type/category of the notification
        public bool isRead { get; set; } // Read status of the notification
        public string redirectUrl { get; set; } // URL to redirect when notification is clicked
        public DateTime createdAt { get; set; } // Timestamp of notification creation
    }
}
