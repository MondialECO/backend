using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class Investments
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string InvestorId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string IdeaId { get; set; }
        public string RoundName { get; set; }
        public decimal Amount { get; set; }
        public double EquityPercentage { get; set; }
        public DateTime InvestedAt { get; set; } = DateTime.UtcNow;

        // Indexes: InvestorId, IdeaId, RoundName
    }

}



