using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class BusinessIdeas
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatorId { get; set; }

        public string CompanyName { get; set; }

        public FounderIdentity FounderIdentity { get; set; }
        public Problem Problem { get; set; }
        public Solution Solution { get; set; }
        public Market Market { get; set; }
        public BusinessModel BusinessModel { get; set; }
        public Operations Operations { get; set; }
        public Roadmap Roadmap { get; set; }
        public Compliance Compliance { get; set; }

        // --- Scoring & Stage ---
        public double ReadinessScore { get; set; }
        public int CurrentStage { get; set; } // 1–5 (Investor stages)
        public string StageLabel { get; set; } // Idea | MVP | Growth

        // --- Investor visibility ---
        public bool IsVisibleToInvestors { get; set; }

        // --- Funding ---
        public decimal FundingRequired { get; set; }
        public double EquityOffered { get; set; }

        // --- Analytics ---
        public long Impressions { get; set; }
        public long Clicks { get; set; }

        // --- Workflow ---
        public string Status { get; set; } // Draft | Submitted | Approved | Rejected

        // --- Embedded ---
        public List<Milestone> Milestones { get; set; } = new();
        public List<InvestmentRound> InvestmentRounds { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    public class FounderIdentity
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string Experience { get; set; }
        public bool LaunchedBefore { get; set; }
        public int WeeklyTimeHours { get; set; }
        public string Motivation { get; set; }
    }

    public class Problem
    {
        public string Description { get; set; }
        public string TargetAudience { get; set; }
        public List<string> CurrentSolutions { get; set; }
        public string Gaps { get; set; }
    }

    public class Solution
    {
        public string Description { get; set; }
        public string Differentiation { get; set; }
        public List<string> Benefits { get; set; }
        public string Vision { get; set; }
    }

    public class Market
    {
        public string PrimaryCustomer { get; set; }
        public string Geography { get; set; }
        public string BuyingBehavior { get; set; }
        public string MarketSize { get; set; }
        public List<string> Competitors { get; set; }
    }

    public class BusinessModel
    {
        public string ProductOrService { get; set; }
        public string Pricing { get; set; }
        public string SalesChannel { get; set; }
        public decimal StartupCosts { get; set; }
        public decimal RevenueTarget12Months { get; set; }
        public List<string> PotentialPartners { get; set; }
    }

    public class Operations
    {
        public string Requirements { get; set; }
        public bool HasPrototype { get; set; }
        public List<string> PlannedTools { get; set; }

        public List<string> Risks { get; set; }
    }

    public class Roadmap
    {
        public string Days30 { get; set; }
        public string Days90 { get; set; }
        public string Year1 { get; set; }
    }

    public class Compliance
    {
        public bool IsRegulated { get; set; }
        public List<string> LegalRisks { get; set; }
        public List<string> Certifications { get; set; }
        public string HelpNeeded { get; set; }
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
