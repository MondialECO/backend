using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class Investments
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string RoundId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string IdeaId { get; set; }
        public string ideaName { get; set; }
        public Guid InvestorId { get; set; }
        public string InvestorName { get; set; }
        public string RoundName { get; set; }
        public decimal Amount { get; set; }
        public double EquityPercentage { get; set; }
        public string Status { get; set; } // Pending | Escrowed | Completed | Refunded
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Indexes: InvestorId, IdeaId, RoundName
    }

}



