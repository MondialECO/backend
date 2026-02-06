using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WebApp.Models.DatabaseModels;

namespace WebApp.Models.Dtos
{
    // DTOs/CreateIdeaDto.cs
    public class CreateIdeaDto
    {
        // Concept Overview
        public string name { get; set; }
        public string? problem_statement { get; set; }
        public string? target_audience { get; set; }
        public string? existing_solutions { get; set; }

        // Proposed Solution
        public string? solution_description { get; set; }
        public string? stage { get; set; } // "idea", "mvp", etc.
        public string? differentiation { get; set; }
        public string? client_benefits { get; set; }
        public string? long_term_vision { get; set; }

        // Market Analysis
        public string? primary_customer_segment { get; set; }
        public string? geographic_target { get; set; }
        public string? purchasing_behavior { get; set; }
        public string? market_size { get; set; }

        // Business Model
        public string? product_type { get; set; } // "product", "service", "product & service"
        public string? planned_price { get; set; }
        public string? sales_channels { get; set; }
        public string? startup_costs { get; set; }
        public string? revenue_12_months { get; set; }

        // Operations
        public string? startup_requirements { get; set; }
        public string? prototype_status { get; set; } // "I Have" or "Haven’t"
        public string? main_risks { get; set; }

        // Roadmap
        public string? goals_30_days { get; set; }
        public string? targets_90_days { get; set; }
        public string? objectives_12_months { get; set; }

        // Risks & Compliance
        public string? regulatory_considerations { get; set; }
        public string? legal_risks { get; set; }
        public string? certifications_licenses { get; set; }

        // Founder
        public string? business_name { get; set; }
        public string? founder_role { get; set; }
        public string? experience_skills { get; set; }
        public string? prior_project_experience { get; set; }
        public string? weekly_time_available { get; set; }
        public string? motivation_vision_statement { get; set; }

        // Equity
        public decimal? amount_required { get; set; }
        public double? equity_percentage { get; set; }

        // Meta
        public string? Id { get; set; } // for draft update
        public string? status { get; set; } // "DRAFT" or "SUBMITTED"

    }

    // File handling (IFormFile)
    public class CreateIdeaFilesDto
    {
        public List<IFormFile>? media { get; set; }
        public List<IFormFile>? documents { get; set; }
    }
















    //public class CreateIdeaDto
    //{
    //    public string Name { get; set; }  // business name
    //    public Problem? Problem { get; set; } // problem being solved
    //    public Solution? Solution { get; set; } // proposed solution
    //    public Market? Market { get; set; } // target market
    //    public BusinessModel? BusinessModel { get; set; }
    //    public Operations? Operations { get; set; }
    //    public Roadmap? Roadmap { get; set; }
    //    public Compliance? Compliance { get; set; }
    //    public FounderIdentity? FounderIdentity { get; set; }

    //    public List<string?> ImageVideoUrls { get; set; } = new();
    //    public List<string?> DocumentUrls { get; set; } = new();
    //    public string? Status { get; set; } // 1–5 (Investor stages)
    //    // --- Funding ---
    //    public decimal? FundingRequired { get; set; }
    //    public double? EquityOffered { get; set; }

    //    public IFormFileCollection? Media { get; set; }
    //    public IFormFileCollection? Documents { get; set; }

    //}

}
