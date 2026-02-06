using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class BusinessIdeas
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string CreatorId { get; set; } // Reference to Users collection

        public string Name { get; set; } // idea name

        public Problem Problem { get; set; } // problem being solved
        public Solution Solution { get; set; } // proposed solution
        public Market Market { get; set; } // target market
        public BusinessModel BusinessModel { get; set; } // business model
        public Operations Operations { get; set; }  // operations plan
        public Roadmap Roadmap { get; set; } // roadmap
        public Compliance Compliance { get; set; } // compliance info
        public FounderIdentity FounderIdentity { get; set; } // founder details
        public List<string> ImageVideo { get; set; } = new(); // image/video URLs
        public List<string> DocumentUrls { get; set; } = new(); // supporting documents

        // --- Funding ---
        public decimal FundingRequired { get; set; } // amount required
        public double EquityOffered { get; set; } // percentage

        public double ReadinessScore { get; set; }
        public string Status { get; set; } // Draft | Submitted | Approved | Rejected
        public bool IsPublished { get; set; } = false; // default to false

        // --- Analytics ---
        public long Impressions { get; set; } // number of views
        public decimal Clicks { get; set; } // number of clicks
        // --- Embedded ---
        public List<Milestone> Milestones { get; set; } = new();
        public List<InvestmentRound> InvestmentRounds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    public class Problem
    {
        public string Description { get; set; }
        public string TargetAudience { get; set; }
        public string ExistingSolutions { get; set; }
        //public string Gaps { get; set; }
    }

    public class Solution
    {
        public string Description { get; set; }
        public string Differentiation { get; set; }
        //public List<string> Benefits { get; set; }
        public string StageLabel { get; set; } // Idea | MVP | Growth
        public string Benefits { get; set; }
        public string Vision { get; set; }
    }

    public class Market
    {
        public string PrimaryCustomer { get; set; }
        public string Geography { get; set; }
        public string BuyingBehavior { get; set; }
        public string MarketSize { get; set; }
        //public List<string> Competitors { get; set; }
        //public string Competitors { get; set; }
    }

    public class BusinessModel
    {
        public string ProductOrService { get; set; }
        public string Pricing { get; set; }
        public string SalesChannel { get; set; }
        public string StartupCosts { get; set; }
        public string RevenueTarget12Months { get; set; }
        //public List<string> PotentialPartners { get; set; }
    }

    public class Operations
    {
        public string Requirements { get; set; }
        public string ProtoType { get; set; }
        //public List<string> PlannedTools { get; set; }

        //public List<string> Risks { get; set; }
        public string Risks { get; set; }
    }

    public class Roadmap
    {
        public string Days30 { get; set; }
        public string Days90 { get; set; }
        public string Year1 { get; set; }
    }

    public class Compliance
    {
        public string IsRegulated { get; set; }
        public string LegalRisks { get; set; }
        public string Certifications { get; set; }
    }

    public class FounderIdentity
    {
        public string BusinessName { get; set; }
        public string Role { get; set; }
        public List<string> Experience { get; set; } = new(); // ex: ["Software Development", "Marketing"]
        public string LaunchedBefore { get; set; }
        public string WeeklyTimeHours { get; set; }
        public string Motivation { get; set; }
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
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string RoundName { get; set; } // Seed, Series A
        public decimal Amount { get; set; }
        public decimal MinInvestment { get; set; }
        public decimal MaxInvestment { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public string Status { get; set; } // Open | Closed
    }
}
