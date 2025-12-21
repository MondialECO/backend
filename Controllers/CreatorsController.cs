using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using System.Security.Claims;
using WebApp.Models.BusinessIdeas;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreatorsController : ControllerBase
    {
        private readonly IBusinessIdeasService _serviceIdea;
        private readonly IInvestmentsService _investmentsService;
        private readonly ITransactionsService _transactionsService;
        public CreatorsController(IBusinessIdeasService service,
            IInvestmentsService investmentsService,
            ITransactionsService transactionsService)
        {
            _serviceIdea = service;
            _investmentsService = investmentsService;
            _transactionsService = transactionsService;
        }


        [HttpGet("dashboard")]
        public async Task<IActionResult> GetCreatorDashboard()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1. Creator Ideas
            var ideas = (await _serviceIdea.GetByCreatorAsync(userId)).ToList();
            var ideaIds = ideas.Select(i => i.Id).ToList();

            // 2. Investments on those ideas
            var investments = ideaIds.Any()
                ? (await _investmentsService.GetByIdeaIdsAsync(ideaIds)).ToList()
                : new List<Investments>();

            // 3. Wallet transactions
            var transactions = (await _transactionsService.GetByUserAsync(userId)).ToList();

            // CALCULATIONS
            var totalIdeas = ideas.Count;

            var totalFundRaised = investments.Sum(i => i.Amount);

            var activeInvestors = investments
                .Select(i => i.InvestorId)
                .Distinct()
                .Count();

            var walletBalance = transactions
                .Where(t => t.Status == "Completed")
                .Sum(t => t.Amount);

            // Idea wise summary
            var ideaSummaries = ideas.Select(i => new
            {
                id = i.Id,
                title = i.Title,
                status = i.Status,
                stage = i.Stage,
                fundingRequired = i.FundingRequired,
                equityOffered = i.EquityOffered,
                totalRaised = investments
                    .Where(inv => inv.IdeaId == i.Id)
                    .Sum(inv => inv.Amount)
            });

            return Ok(new
            {
                totalIdeas,
                totalFundRaised,
                activeInvestors,
                walletBalance,
                ideas = ideaSummaries
            });
        }


        [HttpPost("new-idea")]
        public async Task<IActionResult> CreateBusinessIdea([FromBody] CreateIdeaDto idea)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var data = new BusinessIdeas
            {
                CreatorId = userId,
                Title = idea.Title,
                Summary = idea.Summary,
                Stage = idea.Stage,
                Status = "Pending",
                MarketSize = idea.MarketSize,
                Problem = idea.Problem,
                Solution = idea.Solution,
                RevenueModel = idea.RevenueModel,
                FundingRequired = idea.FundingRequired,
                EquityOffered = idea.EquityOffered,
                Milestones = idea.Milestones?.Select(m => new Milestone
                {
                    Title = m.Title,
                    Description = m.Description,
                    TargetDate = m.TargetDate
                }).ToList() ?? new List<Milestone>()
            };
            data.CreatedAt = DateTime.UtcNow;

            var createdIdea = await _serviceIdea.CreateIdeaAsync(data);
            return Ok(new
            {
                success = true,
                message = "Idea submitted for review",
                ideaId = data.Id
            });
        }

        [HttpGet("my")]
        public async Task<IActionResult> MyIdeas()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var ideas = await _serviceIdea.GetByCreatorAsync(userId);

            if (ideas == null || !ideas.Any())
                return Ok(new List<object>());

            var ideaIds = ideas.Select(i => i.Id).ToList();

            // Get investments for these ideas
            var investments = ideaIds.Any()
                ? await _investmentsService.GetByIdeaIdsAsync(ideaIds)
                : new List<Investments>();

            // Group investments by IdeaId to calculate totalRaised per idea
            var investmentByIdea = investments
                .GroupBy(inv => inv.IdeaId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(inv => inv.Amount)
                );

            // Build response with correct totalRaised for each idea
            var response = ideas.Select(idea => new
            {
                id = idea.Id,
                title = idea.Title ?? "",
                summary = idea.Summary ?? "",
                stage = idea.Stage.ToString(), // "Idea", "MVP", "Growth"
                marketSize = idea.MarketSize ?? "",
                problem = idea.Problem ?? "",
                solution = idea.Solution ?? "",
                revenueModel = idea.RevenueModel ?? "",
                fundingRequired = idea.FundingRequired,
                equityOffered = idea.EquityOffered,
                totalRaised = investmentByIdea.TryGetValue(idea.Id, out var raised) ? raised : 0,
                status = idea.Status.ToString(), // "Pending", "Approved", "Rejected"
                milestones = idea.Milestones != null && idea.Milestones.Any()
                    ? idea.Milestones.Select(m => new
                    {
                        title = m.Title ?? "",
                        description = m.Description ?? "",
                        targetDate = m.TargetDate.ToString("yyyy-MM-dd")
                    }).ToList<object>() 
                    : new List<object>()
            }).ToList();

            return Ok(response);
        }


        [HttpGet("ideas/{id}")]
        public async Task<IActionResult> GetIdea(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var idea = await _serviceIdea.GetByIdAsync(id);

            if (idea == null || idea.CreatorId != userId)
                return NotFound();


            // Investments for totalRaised
            var investments = await _investmentsService.GetByIdeaAsync(id);
            var totalRaised = investments.Sum(i => i.Amount);

      

            var response = new
            {
                id = idea.Id,
                title = idea.Title ?? "",
                summary = idea.Summary ?? "",
                stage = idea.Stage.ToString(),
                marketSize = idea.MarketSize ?? "",
                problem = idea.Problem ?? "",
                solution = idea.Solution ?? "",
                revenueModel = idea.RevenueModel ?? "",
                fundingRequired = idea.FundingRequired,
                equityOffered = idea.EquityOffered,
                totalRaised = totalRaised,
                status = idea.Status.ToString(),
                milestones = idea.Milestones != null && idea.Milestones.Any()
                    ? idea.Milestones.Select(m => new
                    {
                        title = m.Title ?? "",
                        description = m.Description ?? "",
                        targetDate = m.TargetDate.ToString("yyyy-MM-dd")
                    }).ToList<object>()
                    : new List<object>()
            };

            return Ok(response);
        }


        //[HttpPut("update/{id}")]
        //public async Task<IActionResult> UpdateIdea(string id, [FromBody] UpdateIdeaRequest request)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var existingIdea = await _serviceIdea.GetByIdAsync(id);

        //    if (existingIdea == null || existingIdea.CreatorId != userId)
        //        return NotFound("Idea not found or you don't have permission to edit it.");

        //    // Update the fields
        //    existingIdea.Title = request.Title ?? existingIdea.Title;
        //    existingIdea.Summary = request.Summary ?? existingIdea.Summary;
        //    existingIdea.Stage = request.Stage;
        //    existingIdea.MarketSize = request.MarketSize ?? existingIdea.MarketSize;
        //    existingIdea.Problem = request.Problem ?? existingIdea.Problem;
        //    existingIdea.Solution = request.Solution ?? existingIdea.Solution;
        //    existingIdea.RevenueModel = request.RevenueModel ?? existingIdea.RevenueModel;
        //    existingIdea.FundingRequired = request.FundingRequired;
        //    existingIdea.EquityOffered = request.EquityOffered;

        //    // Update milestones (simple replace — delete old, add new)
        //    if (existingIdea.Milestones != null)
        //    {
        //        _context.IdeaMilestones.RemoveRange(existingIdea.Milestones);
        //    }

        //    if (request.Milestones != null && request.Milestones.Any())
        //    {
        //        existingIdea.Milestones = request.Milestones.Select(m => new IdeaMilestone
        //        {
        //            Title = m.Title,
        //            Description = m.Description,
        //            TargetDate = DateTime.Parse(m.TargetDate)
        //        }).ToList();
        //    }

        //    await _serviceIdea.UpdateAsync(existingIdea);

        //    return Ok(new { message = "Idea updated successfully!" });
        //}

        //public class UpdateIdeaRequest
        //{
        //    public string? Title { get; set; }
        //    public string? Summary { get; set; }
        //    public IdeaStage Stage { get; set; }
        //    public string? MarketSize { get; set; }
        //    public string? Problem { get; set; }
        //    public string? Solution { get; set; }
        //    public string? RevenueModel { get; set; }
        //    public decimal FundingRequired { get; set; }
        //    public decimal EquityOffered { get; set; }
        //    public List<MilestoneRequest>? Milestones { get; set; }
        //}

        //public class MilestoneRequest
        //{
        //    public string Title { get; set; } = string.Empty;
        //    public string Description { get; set; } = string.Empty;
        //    public string TargetDate { get; set; } // "yyyy-MM-dd"
        //}
    }
}
