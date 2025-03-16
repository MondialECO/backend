using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace WebApp.Models.DatabaseModels
{
    public class BlogCommentModel
    {

        [BsonId]
        public string Id { get; set; }
        public string BlogPostId { get; set; }
        public string ParentCommentId { get; set; }
        public string UserId { get; set; }
        public string Comment { get; set; }
        public DateTime CommentDate { get; set; } = DateTime.UtcNow;
    }
}
