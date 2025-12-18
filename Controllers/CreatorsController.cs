using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using System.Security.Claims;
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
            Console.WriteLine(ideas);
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


    }
}
