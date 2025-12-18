using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class BusinessIdeas
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? CreatorId { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Problem { get; set; }
        public string Solution { get; set; }
        public string MarketSize { get; set; }
        public string RevenueModel { get; set; }
        public decimal FundingRequired { get; set; } // Use decimal for money
        public double EquityOffered { get; set; }
        public string Stage { get; set; } // Idea | MVP | Growth
        public string Status { get; set; } // Pending | Approved | Rejected

        public List<Milestone> Milestones { get; set; } = new();
        public List<InvestmentRound> InvestmentRounds { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Indexes:
        // 1. CreatorId
        // 2. Status
        // 3. Stage
    }


    public class Milestone
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime TargetDate { get; set; }
        public string Status { get; set; } // Pending | Completed
    }

    public class InvestmentRound
    {
        public string RoundName { get; set; } // Seed, Series A
        public decimal TargetAmount { get; set; }
        public decimal MinInvestment { get; set; }
        public decimal MaxInvestment { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public string Status { get; set; } // Open | Closed
    }
}
