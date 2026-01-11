using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Models.DatabaseModels;
using WebApp.Models.Dtos;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FounderController : ControllerBase
    {
        private readonly IBusinessIdeasService _serviceIdea;
        private readonly IInvestmentsService _investmentsService;
        private readonly ITransactionsService _transactionsService;
        public FounderController(IBusinessIdeasService service,
            IInvestmentsService investmentsService,
            ITransactionsService transactionsService)
        {
            _serviceIdea = service;
            _investmentsService = investmentsService;
            _transactionsService = transactionsService;
        }


        //[HttpGet("dashboard")]
        //public async Task<IActionResult> GetCreatorDashboard()
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    // 1. Creator Ideas
        //    var ideas = (await _serviceIdea.GetByCreatorAsync(userId)).ToList();
        //    var ideaIds = ideas.Select(i => i.Id).ToList();

        //    // 2. Investments on those ideas
        //    var investments = ideaIds.Any()
        //        ? (await _investmentsService.GetByIdeaIdsAsync(ideaIds)).ToList()
        //        : new List<Investments>();

        //    // 3. Wallet transactions
        //    var transactions = (await _transactionsService.GetByUserAsync(userId)).ToList();

        //    // CALCULATIONS
        //    var totalIdeas = ideas.Count;

        //    var totalFundRaised = investments.Sum(i => i.Amount);

        //    var activeInvestors = investments
        //        .Select(i => i.InvestorId)
        //        .Distinct()
        //        .Count();

        //    var walletBalance = transactions
        //        .Where(t => t.Status == "Completed")
        //        .Sum(t => t.Amount);

        //    // Idea wise summary
        //    var ideaSummaries = ideas.Select(i => new
        //    {
        //        id = i.Id,
        //        //title = i.Title,
        //        status = i.Status,
        //        //stage = i.Stage,
        //        fundingRequired = i.FundingRequired,
        //        equityOffered = i.EquityOffered,
        //        totalRaised = investments
        //            .Where(inv => inv.IdeaId == i.Id)
        //            .Sum(inv => inv.Amount)
        //    });

        //    return Ok(new
        //    {
        //        totalIdeas,
        //        totalFundRaised,
        //        activeInvestors,
        //        walletBalance,
        //        ideas = ideaSummaries
        //    });
        //}

        // new draft or idea
        [HttpPost("new-idea")]
        public async Task<IActionResult> CreateBusinessIdea([FromBody] CreateIdeaDto idea)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var createdIdea = await _serviceIdea.CreateIdeaAsync(idea, userId);
            return Ok(new
            {
                success = true,
                message = "Draft created/saved",
            });
        }

        // draft or idea update 
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIdea([FromBody] UpdateIdeaDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _serviceIdea.UpdateIdeaAsync(dto, userId);

            return result.Match<IActionResult>(
                idea => Ok(new { success = true, message = "Idea updated" }),
                err => BadRequest(err)
            );
        }

        //[HttpGet("my")]
        //public async Task<IActionResult> MyIdeas()
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var ideas = await _serviceIdea.GetByCreatorAsync(userId);

        //    if (ideas == null || !ideas.Any())
        //        return Ok(new List<object>());

        //    var ideaIds = ideas.Select(i => i.Id).ToList();

        //    // Get investments for these ideas
        //    var investments = ideaIds.Any()
        //        ? await _investmentsService.GetByIdeaIdsAsync(ideaIds)
        //        : new List<Investments>();

        //    // Group investments by IdeaId to calculate totalRaised per idea
        //    var investmentByIdea = investments
        //        .GroupBy(inv => inv.IdeaId)
        //        .ToDictionary(
        //            g => g.Key,
        //            g => g.Sum(inv => inv.Amount)
        //        );

        //    // Build response with correct totalRaised for each idea
        //    var response = ideas.Select(idea => new
        //    {
        //        id = idea.Id,
        //        title = idea.Title ?? "",
        //        summary = idea.Summary ?? "",
        //        stage = idea.Stage.ToString(), // "Idea", "MVP", "Growth"
        //        marketSize = idea.MarketSize ?? "",
        //        problem = idea.Problem ?? "",
        //        solution = idea.Solution ?? "",
        //        revenueModel = idea.RevenueModel ?? "",
        //        fundingRequired = idea.FundingRequired,
        //        equityOffered = idea.EquityOffered,
        //        totalRaised = investmentByIdea.TryGetValue(idea.Id, out var raised) ? raised : 0,
        //        status = idea.Status.ToString(), // "Pending", "Approved", "Rejected"
        //        milestones = idea.Milestones != null && idea.Milestones.Any()
        //            ? idea.Milestones.Select(m => new
        //            {
        //                title = m.Title ?? "",
        //                description = m.Description ?? "",
        //                targetDate = m.TargetDate.ToString("yyyy-MM-dd")
        //            }).ToList<object>()
        //            : new List<object>()
        //    }).ToList();

        //    return Ok(response);
        //}


        //[HttpGet("ideas/{id}")]
        //public async Task<IActionResult> GetIdea(string id)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var idea = await _serviceIdea.GetByIdAsync(id);

        //    if (idea == null || idea.CreatorId != userId)
        //        return NotFound();


        //    // Investments for totalRaised
        //    var investments = await _investmentsService.GetByIdeaAsync(id);
        //    var totalRaised = investments.Sum(i => i.Amount);



        //    var response = new
        //    {
        //        id = idea.Id,
        //        title = idea.Title ?? "",
        //        summary = idea.Summary ?? "",
        //        stage = idea.Stage.ToString(),
        //        marketSize = idea.MarketSize ?? "",
        //        problem = idea.Problem ?? "",
        //        solution = idea.Solution ?? "",
        //        revenueModel = idea.RevenueModel ?? "",
        //        fundingRequired = idea.FundingRequired,
        //        equityOffered = idea.EquityOffered,
        //        totalRaised = totalRaised,
        //        status = idea.Status.ToString(),
        //        milestones = idea.Milestones != null && idea.Milestones.Any()
        //            ? idea.Milestones.Select(m => new
        //            {
        //                title = m.Title ?? "",
        //                description = m.Description ?? "",
        //                targetDate = m.TargetDate.ToString("yyyy-MM-dd")
        //            }).ToList<object>()
        //            : new List<object>()
        //    };

        //    return Ok(response);
        //}



        [HttpGet("{id}/investments")]
        public async Task<IActionResult> GetIdeaInvestments(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var idea = await _serviceIdea.GetByIdAsync(id);
            if (idea == null || idea.CreatorId != userId) return NotFound();

            var investments = await _investmentsService.GetByIdeaAsync(id);
            if (investments == null || !investments.Any())
                return Ok(new List<object>());

            var response = investments.Select(inv => new
            {
                id = inv.Id,
                investorName = inv.InvestorName,
                investedAmount = inv.Amount,
                equityPercentage = inv.EquityPercentage,
                investedDate = inv.InvestedAt.ToString("yyyy-MM-dd"),
                roundName = inv.RoundName ?? "Seed Round"
            }).ToList();

            return Ok(response);
        }


        [HttpPut("ideas/{id}")]
        public async Task<IActionResult> UpdateIdea(string id, [FromBody] CreateIdeaDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var existingIdea = await _serviceIdea.GetByIdAsync(id);

            if (existingIdea == null || existingIdea.CreatorId != userId)
                return NotFound("Idea not found or you don't have permission to edit it.");

            await _serviceIdea.UpdateIdeaAsync(existingIdea, request);

            return Ok(new { message = "Idea updated successfully!" });
        }


    }
}
