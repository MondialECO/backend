using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class BusinessIdeas
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string CreatorId { get; set; }
        public string CompanyName { get; set; }
        public string Stage { get; set; }
        public string ReadinessScore { get; set; }

        // founder Identity
        public FounderIdentity FounderIdentity { get; set; }

        // problem
        public Problem Problem { get; set; }


        public string Title { get; set; }
        public string Summary { get; set; }
        //public string Problem { get; set; }
        public string Solution { get; set; }
        public string MarketSize { get; set; }
        public string RevenueModel { get; set; }
        public decimal FundingRequired { get; set; } // Use decimal for money
        public double EquityOffered { get; set; }
        //public string Stage { get; set; } // Idea | MVP | Growth
        public string Status { get; set; } // Pending | Approved | Rejected

        public List<Milestone> Milestones { get; set; } = new();
        public List<InvestmentRound> InvestmentRounds { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Indexes:
        // 1. CreatorId
        // 2. Status
        // 3. Stage
    }

    public class FounderIdentity
    { 
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Experience { get; set; }
        public bool LaunchedBefore { get; set; }
        public string WeeklyTime { get; set; }
        public string Motivation { get; set; }
    }

    public class Problem
    { 
        public string Description { get; set; }
        public string AffectedUsers { get; set; }
        public string CurrentSolutions  { get; set; }
        public string gaps  { get; set; }
    }

    public class Solution
    {
        public string Description { get; set;}
        public string CurrentSolutions { get; set; }
        public string benefits { get; set; }
        public string vision { get; set; }
    }

    public class Market
    {
        public string PrimaryCustomer { get; set; }
        public string Geography { get; set; }
        public string BuyingBehavior { get; set; }
        public string MarketSize { get; set; }
        public string Competitors { get; set; } // competitors: ["X", "Y"]
    }

   public class businessModel
    {
        public string FirstOffer { get; set; }
        public string Pricing { get; set; }
        public string SalesChannel { get; set; }
        public string StatupCosts { get; set; }
        public string Revenue12m { get; set; }
        public string Partners { get; set; } // partners: ["Advisor A"]
    }

    public class Operations
    {
        public string Requirements { get; set; } // requirements: "Tools, team",
        public string hasPrototype { get; set; }
        public string tools { get; set; } // tools: ["Figma", "Stripe"],
        public string risks { get; set; } // risks: ["Tech", "Legal"]
    }

    public class Roadmap
    {
        public string d30 { get; set; }
        public string d90 { get; set; }
        public string y1 { get; set; }
    }

    public class Compliance
    {
        public bool regulated { get; set; }
        public string legalRisks { get; set; }
        public string certifications { get; set; }
        public string helpNeeded { get; set; }
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
