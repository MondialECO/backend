using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Org.BouncyCastle.Crypto;
using System.Linq;
using System.Security.Claims;
using WebApp.DbContext;
using WebApp.Models.DatabaseModels;
using WebApp.Models.Dtos;
using WebApp.Services;
using WebApp.Services.Interface;


namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreatorController : ControllerBase
    {
        private readonly IBusinessIdeasService _serviceIdea;
        private readonly IInvestmentsService _investmentsService;
        private readonly ITransactionsService _transactionsService;
        private readonly SaveFile _saveFile;
        private readonly MongoDbContext _context;
        public CreatorController(IBusinessIdeasService service,
            IInvestmentsService investmentsService,
            ITransactionsService transactionsService,
             SaveFile saveFile,
             MongoDbContext context)
        {
            _serviceIdea = service;
            _investmentsService = investmentsService;
            _transactionsService = transactionsService;
            _saveFile = saveFile;
            _context = context;
        }


        [HttpGet("dashboard")]
        public async Task<IActionResult> GetCreatorDashboard()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1️ Get Creator Ideas
            var ideas = (await _serviceIdea.GetByCreatorAsync(userId)).ToList();
            var ideaIds = ideas.Select(i => i.Id).ToList();

            var totalIdeas = ideas.Count;

            // 2️ Click Analytics (Last 14 Days)
            var last14Days = DateTime.UtcNow.AddDays(-14);

            var totalClicksLast14Days = ideaIds.Any()
                ? await _context.IdeaClicks.CountDocumentsAsync(x =>
                    ideaIds.Contains(x.IdeaId) &&
                    x.ClickedAt >= last14Days)
                : 0;

            // 3️ Investments
            var investments = ideaIds.Any()
                ? (await _investmentsService.GetByIdeaIdsAsync(ideaIds)).ToList()
                : new List<Investments>();

            var totalFundRaised = investments.Sum(i => i.Amount);
            var totalRequired = ideas.Sum(i => i.FundingRequired);
            var totalEquity = ideas.Sum(i => i.EquityOffered);

            var activeInvestors = investments
                .Select(i => i.InvestorId)
                .Distinct()
                .Count();

            // 4️ Investor Info
            var investorIds = investments
                .Select(i => i.InvestorId)
                .Distinct()
                .ToList();

            var investors = investorIds.Any()
                ? await _context.ApplicationUsers
                    .Find(x => investorIds.Contains(x.Id))
                    .ToListAsync()
                : new List<ApplicationUser>();

            var investorDictionary = investors
                .ToDictionary(x => x.Id);

            // 5️ Optimize Investment Grouping 
            var investmentGrouped = investments
                .GroupBy(x => x.IdeaId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 6️ Idea Wise Summary
            var ideaSummaries = ideas.Select(idea =>
            {
                var ideaInvestments = investmentGrouped.ContainsKey(idea.Id)
                    ? investmentGrouped[idea.Id]
                    : new List<Investments>();

                return new
                {
                    id = idea.Id,
                    name = idea.Name,
                    status = idea.Status,
                    stageLabel = idea.Solution?.StageLabel,
                    isPublished = idea.IsPublished,
                    createdAt = idea.CreatedAt,

                    fundingRequired = idea.FundingRequired,
                    equityOffered = idea.EquityOffered,

                    totalRaised = ideaInvestments.Sum(inv => inv.Amount),

                    fundingProgress = idea.FundingRequired > 0
                        ? Math.Round((ideaInvestments.Sum(inv => inv.Amount) / idea.FundingRequired) * 100, 2)
                        : 0,

                    investors = ideaInvestments
                        .Select(inv =>
                        {
                            var user = investorDictionary.ContainsKey(inv.InvestorId)
                                ? investorDictionary[inv.InvestorId]
                                : null;

                            return user == null ? null : new
                            {
                                investorId = user.Id,
                                name = user.Name,
                                imageUrl = user.ImagePath,
                                ideaName = inv.ideaName,
                                investedAmount = inv.Amount
                            };
                        })
                        .Where(x => x != null)
                        .ToList()
                };
            }).ToList();

            // 7️ Final Response
            var response = new
            {
                totalIdeas,
                totalClicksLast14Days,
                totalFundRaised,
                totalRequired,
                totalEquity,
                activeInvestors,
                ideas = ideaSummaries
            };

            return Ok(response);
        }


        // create new business idea or save draft
        [HttpPost("new-idea/{id?}")]
        public async Task<IActionResult> CreateOrUpdateIdea(
            [FromForm] CreateIdeaDto dto,
            [FromForm] List<IFormFile>? media,
            [FromForm] List<IFormFile>? documents,
            string? id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();



            // Save media files
            var mediaPaths = new List<string>();
            if (media != null && media.Any())
            {
                foreach (var file in media)
                {
                    var path = await _saveFile.SaveFileAsync(file, "media");
                    mediaPaths.Add(path);
                }
            }

            // Save document files
            var documentPaths = new List<string>();
            if (documents != null && documents.Any())
            {
                foreach (var file in documents)
                {
                    var path = await _saveFile.SaveFileAsync(file, "documents");
                    documentPaths.Add(path);
                }
            }


            string ideaId;

            if (string.IsNullOrEmpty(id))
            {
                var createdIdea = await _serviceIdea.CreateIdeaAsync(dto, userId, mediaPaths, documentPaths);
                ideaId = createdIdea.Id;
            }
            else
            {
                await _serviceIdea.UpdateIdeaAsync(dto, userId, id, mediaPaths, documentPaths);
                ideaId = id;
            }

            return Ok(new
            {
                success = true,
                message = "Draft saved",
                id = ideaId
            });
        }



        // get idea by id
        [HttpGet("idea/{id}")]
        public async Task<IActionResult> GetIdea(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var idea = await _serviceIdea.GetByIdAsync(id);
            if (idea == null || idea.CreatorId != userId)
                return NotFound();

            var responce = new
            {
                id = idea.Id,
                name = idea.Name,
                problem = idea.Problem,
                solution = idea.Solution,
                market = idea.Market,
                businessModel = idea.BusinessModel,
                operations = idea.Operations,
                roadmap = idea.Roadmap,
                compliance = idea.Compliance,
                founderIdentity = idea.FounderIdentity,
                isPublished = idea.IsPublished,
                fundingRequired = idea.FundingRequired,
                equityOffered = idea.EquityOffered,
                status = idea.Status,
                imageVideoUrls = idea.ImageVideo,
                documentUrls = idea.DocumentUrls,
                createAt = idea.CreatedAt,
                updateAt = idea.UpdatedAt

            };

            return Ok(responce);
        }


        [HttpGet("my-ideas")]
        public async Task<IActionResult> MyIdeas()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var ideas = await _serviceIdea.GetByCreatorAsync(userId);

            if (ideas == null || !ideas.Any())
                return Ok(new List<object>());

            var ideaIds = ideas.Select(i => i.Id).ToList();

            var investments = ideaIds.Any()
            ? (await _investmentsService.GetByIdeaIdsAsync(ideaIds)).ToList()
            : new List<Investments>();

            var investorIds = investments
               .Select(i => i.InvestorId)
               .Distinct()
               .ToList();

            var investors = investorIds.Any()
                ? await _context.ApplicationUsers
                    .Find(x => investorIds.Contains(x.Id))
                    .ToListAsync()
                : new List<ApplicationUser>();

            var investorDictionary = investors
                .ToDictionary(x => x.Id);

            var investmentGrouped = investments
                .GroupBy(x => x.IdeaId)
                .ToDictionary(g => g.Key, g => g.ToList());
            

            var clickCounts = await _context.IdeaClicks
                .Aggregate()
                .Match(c => c.UserId == userId)
                .Group(
                    c => c.IdeaId,
                    g => new
                    {
                        IdeaId = g.Key,
                        Count = g.Count()
                    })
                .ToListAsync();

            var clickDictionary = clickCounts.ToDictionary(x => x.IdeaId, x => x.Count);


            // Build response with correct totalRaised for each idea
            var response = ideas.Select(idea =>
            {
                investmentGrouped.TryGetValue(idea.Id, out var ideaInvestments);

                ideaInvestments ??= new List<Investments>();

                var totalRaised = ideaInvestments.Sum(x => x.Amount);

                var totalEquity = ideaInvestments.Sum(x => x.EquityPercentage);

                var investorList = ideaInvestments
                    .Select(inv => investorDictionary.TryGetValue(inv.InvestorId, out var investor)
                        ? new
                        {
                            id = investor.Id,
                            name = investor.Name,
                            ImagePath = investor.ImagePath,
                        }
                        : null)
                    .Where(x => x != null)
                    .Distinct()
                    .ToList();

                return new
                {
                    id = idea.Id,
                    name = idea.Name,
                    status = idea.Status,
                    score = idea.ReadinessScore,
                    stageLabel = idea.Solution.StageLabel,
                    isPublished = idea.IsPublished,
                    creatdate = idea.CreatedAt,
                    marketSize = idea.Market?.MarketSize,
                    equityOffered = idea.EquityOffered,

                    views = clickDictionary.TryGetValue(idea.Id, out var totalClick)
                        ? totalClick
                        : 0,

                    fundingRequired = idea.FundingRequired,

                    totalRaised = totalRaised,

                    equity = totalEquity,

                    investors = investorList
                };
            }).ToList();

            return Ok(response);
        }




        [HttpGet("investments")]
        public async Task<IActionResult> GetIdeaInvestments()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ideas = await _serviceIdea.GetByCreatorAsync(userId);
            if (ideas == null || !ideas.Any())
                return Ok(new List<object>());

            var ideaIds = ideas.Select(i => i.Id).ToList();

            var investor = await _investmentsService.GetByIdeaIdsAsync(ideaIds);
            if (investor == null || investor.CreatorId != userId) return NotFound();

            var investments = await _investmentsService.GetByIdeaAsync(id);
            if (investments == null || !investments.Any())
                return Ok(new List<object>());

            var response = investments.Select(inv => new
            {
                id = inv.Id,
                investorName = inv.InvestorName,
                investedAmount = inv.Amount,
                equityPercentage = inv.EquityPercentage,
                investedDate = inv.CreatedAt.ToString("yyyy-MM-dd"),
                roundName = inv.RoundName ?? "Seed Round"
            }).ToList();

            return Ok(response);
        }


        

    }
}
