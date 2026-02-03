using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WebApp.Models.DatabaseModels;

namespace WebApp.Models.Dtos
{
    public class CreateIdeaDto
    {
        public string Name { get; set; }  // business name
        public Problem? Problem { get; set; } // problem being solved
        public Solution? Solution { get; set; } // proposed solution
        public Market? Market { get; set; } // target market
        public BusinessModel? BusinessModel { get; set; }
        public Operations? Operations { get; set; }
        public Roadmap? Roadmap { get; set; }
        public Compliance? Compliance { get; set; }
        public FounderIdentity? FounderIdentity { get; set; }

        public List<string?> ImageVideoUrls { get; set; } = new();
        public List<string?> DocumentUrls { get; set; } = new();
        public string? Status { get; set; } // 1–5 (Investor stages)
        // --- Funding ---
        public decimal? FundingRequired { get; set; }
        public double? EquityOffered { get; set; }

        public IFormFileCollection? Media { get; set; }
        public IFormFileCollection? Documents { get; set; }

    }

}
