using Org.BouncyCastle.Asn1.Ocsp;
using WebApp.Models.DatabaseModels;
using WebApp.Models.Dtos;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Services
{
    public class BusinessIdeasService : IBusinessIdeasService
    {
        private readonly BusinessIdeasRepository _repo;

        public BusinessIdeasService(BusinessIdeasRepository repo)
        {
            _repo = repo;
        }

        // Create or draft idea
        public async Task<BusinessIdeas> CreateIdeaAsync(CreateIdeaDto idea, string userid, List<string> mediaPaths, List<string> documentPaths)
        {
            var data = new BusinessIdeas
            {
                CreatorId = userid,
                Name = idea.name,
                Problem = new Problem
                {
                    Description = idea.problem_statement,
                    TargetAudience = idea.target_audience,
                    ExistingSolutions = idea.existing_solutions,
                },
                Solution = new Solution
                {
                    Description = idea.solution_description,
                    Differentiation = idea.differentiation,
                    StageLabel = idea.stage,
                    Benefits = idea.client_benefits,
                    Vision = idea.long_term_vision,
                },
                Market = new Market
                {
                    PrimaryCustomer = idea.primary_customer_segment,
                    Geography = idea.geographic_target,
                    BuyingBehavior = idea.purchasing_behavior,
                    MarketSize = idea.market_size,
                },
                BusinessModel = new BusinessModel
                {
                    ProductOrService = idea.product_type,
                    Pricing = idea.planned_price,
                    SalesChannel = idea.sales_channels,
                    StartupCosts = idea.startup_costs,
                    RevenueTarget12Months = idea.revenue_12_months,
                },
                Operations = new Operations
                {
                    Requirements = idea.startup_requirements,
                    ProtoType = idea.prototype_status,
                    Risks = idea.main_risks,
                },
                Roadmap = new Roadmap
                {
                    Days30 = idea.goals_30_days,
                    Days90 = idea.goals_30_days,
                    Year1 = idea.objectives_12_months,
                },
                Compliance = new Compliance
                {
                    IsRegulated = idea.regulatory_considerations,
                    LegalRisks = idea.regulatory_considerations,
                    Certifications = idea.regulatory_considerations,
                },
                FounderIdentity = new FounderIdentity
                {
                    BusinessName = idea.business_name,
                    Role = idea.founder_role,
                    //Experience = idea.experience_skills,
                    LaunchedBefore = idea.prior_project_experience,
                    WeeklyTimeHours = idea.weekly_time_available,
                    Motivation = idea.motivation_vision_statement,

                },
                ImageVideo = mediaPaths,
                DocumentUrls = documentPaths,
                Status = idea.status ?? "Draft", // Draft | Submitted | Approved | Rejected
                FundingRequired = idea.amount_required ?? 0,
                EquityOffered = idea.equity_percentage ?? 0,

                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(data);
            return data;
        }

        // Update idea 
        public async Task<BusinessIdeas> UpdateIdeaAsync(CreateIdeaDto idea, string userid, string id, List<string> mediaPaths, List<string> documentPaths)
        {
            var existingIdeas = await _repo.GetByIdeaDriftAsync(id, userid);

            var data = new BusinessIdeas
            {
                CreatorId = existingIdeas.CreatorId,
                Name =idea.name ?? existingIdeas.Name,
                Problem = new Problem
                {
                    Description = idea.problem_statement ?? existingIdeas.Problem.Description,
                    TargetAudience = idea.target_audience ?? existingIdeas.Problem.TargetAudience,
                    ExistingSolutions = idea.existing_solutions ?? existingIdeas.Problem.ExistingSolutions,
                },
                Solution = new Solution
                {
                    Description = idea.solution_description ?? existingIdeas.Solution.Description,
                    Differentiation = idea.differentiation ?? existingIdeas.Solution.Differentiation,
                    StageLabel = idea.stage ?? existingIdeas.Solution.StageLabel,
                    Benefits = idea.client_benefits ?? existingIdeas.Solution.Benefits,
                    Vision = idea.long_term_vision ?? existingIdeas.Solution.Vision,
                },
                Market = new Market
                {
                    PrimaryCustomer = idea.primary_customer_segment ?? existingIdeas.Market.PrimaryCustomer,
                    Geography = idea.geographic_target ?? existingIdeas.Market.Geography,
                    BuyingBehavior = idea.purchasing_behavior ?? existingIdeas.Market.BuyingBehavior,
                    MarketSize = idea.market_size ?? existingIdeas.Market.MarketSize,
                },
                BusinessModel = new BusinessModel
                {
                    ProductOrService = idea.product_type ?? existingIdeas.BusinessModel.ProductOrService,
                    Pricing = idea.planned_price ?? existingIdeas.BusinessModel.Pricing,
                    SalesChannel = idea.sales_channels ?? existingIdeas.BusinessModel.SalesChannel,
                    StartupCosts = idea.startup_costs ?? existingIdeas.BusinessModel.StartupCosts,
                    RevenueTarget12Months = idea.revenue_12_months ?? existingIdeas.BusinessModel.RevenueTarget12Months,
                },
                Operations = new Operations
                {
                    Requirements = idea.startup_requirements ?? existingIdeas.Operations.Requirements,
                    ProtoType = idea.prototype_status ?? existingIdeas.Operations.ProtoType,
                    Risks = idea.main_risks ?? existingIdeas.Operations.Risks,
                },
                Roadmap = new Roadmap
                {
                    Days30 = idea.goals_30_days ?? existingIdeas.Roadmap.Days30,
                    Days90 = idea.goals_30_days ?? existingIdeas.Roadmap.Days90,
                    Year1 = idea.objectives_12_months ?? existingIdeas.Roadmap.Year1,
                },
                Compliance = new Compliance
                {
                    IsRegulated = idea.regulatory_considerations ?? existingIdeas.Compliance.IsRegulated,
                    LegalRisks = idea.regulatory_considerations ?? existingIdeas.Compliance.LegalRisks,
                    Certifications = idea.regulatory_considerations ?? existingIdeas.Compliance.Certifications,
                },
                FounderIdentity = new FounderIdentity
                {
                    BusinessName = idea.business_name ?? existingIdeas.FounderIdentity.BusinessName,
                    Role = idea.founder_role ?? existingIdeas.FounderIdentity.Role,
                    //Experience = idea.experience_skills,
                    LaunchedBefore = idea.prior_project_experience ?? existingIdeas.FounderIdentity.LaunchedBefore,
                    WeeklyTimeHours = idea.weekly_time_available ?? existingIdeas.FounderIdentity.WeeklyTimeHours,
                    Motivation = idea.motivation_vision_statement ?? existingIdeas.FounderIdentity.Motivation,

                },



                Impressions = existingIdeas.Impressions,
                Clicks = existingIdeas.Clicks,
                Status = existingIdeas.Status,
                ImageVideo = mediaPaths ?? existingIdeas.ImageVideo,
                DocumentUrls = documentPaths ?? existingIdeas.DocumentUrls,
                FundingRequired = idea.amount_required ?? existingIdeas.FundingRequired,
                EquityOffered = idea.equity_percentage ?? existingIdeas.EquityOffered,
                UpdatedAt = DateTime.UtcNow


                //Milestones = idea.Milestones?.Select(m => new Milestone
                //{
                //    Title = m.Title,
                //    Description = m.Description,
                //    TargetDate = m.TargetDate
                //}).ToList() ?? new List<Milestone>(),

                //CreatedAt = DateTime.UtcNow,

            };

            await _repo.UpdateAsync(id, data);
            return data;
        }

        // Get ideas by creator
        public async Task<IEnumerable<BusinessIdeas>> GetByCreatorAsync(string creatorId)
        {
            return await _repo.GetByCreatorIdAsync(creatorId);
        }

        // Delete idea
        public async Task DeleteIdeaAsync(string id)
        {
            await _repo.DeleteAsync(id);
        }

        // Get all ideas for admin dashboard
        public async Task<IEnumerable<BusinessIdeas>> GetAllIdeasAsync()
        {
            return await _repo.GetAllAsync();
        }

        // Get idea by id for admin moderation
        public async Task<BusinessIdeas> GetByIdAsync(string id)
        {
            return await _repo.GetByIdAsync(id);
        }

        // Get pending ideas for admin moderation
        public async Task<IEnumerable<BusinessIdeas>> GetPendingIdeasAsync()
        {
            return await _repo.GetPendingIdeasAsync();
        }

       



    }
}
