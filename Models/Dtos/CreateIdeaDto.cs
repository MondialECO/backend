using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WebApp.Models.DatabaseModels;

namespace WebApp.Models.Dtos
{
    public class CreateIdeaDto
    {
        public string? CompanyName { get; set; }

        public FounderIdentity? FounderIdentity { get; set; }
        public Problem? Problem { get; set; }
        public Solution? Solution { get; set; }
        public Market? Market { get; set; }
        public BusinessModel? BusinessModel { get; set; }
        public Operations? Operations { get; set; }
        public Roadmap? Roadmap { get; set; }
        public Compliance? Compliance { get; set; }

        // --- Scoring & Stage ---
        public double? ReadinessScore { get; set; } 
        public int? CurrentStage { get; set; } // 1–5 (Investor stages)
        public string? StageLabel { get; set; } // Idea | MVP | Growth

        // --- Investor visibility ---
        public bool IsVisibleToInvestors { get; set; }

        // --- Funding ---
        public decimal? FundingRequired { get; set; }
        public double? EquityOffered { get; set; }

        // --- Analytics ---
        public long? Impressions { get; set; }
        public long? Clicks { get; set; }

        // --- Workflow ---
        public string? Status { get; set; } // Draft | Submitted | Approved | Rejected

        // --- Embedded ---
        //public List<Milestone?> Milestones { get; set; } = new();
        //public List<InvestmentRound?> InvestmentRounds { get; set; } = new();

    }

}
