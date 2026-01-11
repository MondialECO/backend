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

        public async Task<BusinessIdeas> CreateIdeaAsync(CreateIdeaDto idea, string userid)
        {
            var data = new BusinessIdeas
            {
                CreatorId = userid,
                CompanyName = idea.CompanyName ?? "Untitled Idea",
                FounderIdentity = idea.FounderIdentity,
                Problem = idea.Problem,
                Solution = idea.Solution,
                Market = idea.Market,
                BusinessModel = idea.BusinessModel,
                Operations = idea.Operations,
                Roadmap = idea.Roadmap,
                Compliance = idea.Compliance,
                ReadinessScore = idea.ReadinessScore ?? 0,
                CurrentStage = idea.CurrentStage ?? 1, // Default to stage 1
                StageLabel = idea.StageLabel ?? "Idea",
                IsVisibleToInvestors = false,
                FundingRequired = idea.FundingRequired ?? 0,
                EquityOffered = idea.EquityOffered ?? 0,
                Impressions = idea.Impressions ?? 0,
                Clicks = idea.Clicks ?? 0,
                Status = idea.Status ?? "Draft", // Draft | Submitted | Approved | Rejected

                //Milestones = idea.Milestones?.Select(m => new Milestone
                //{
                //    Title = m.Title,
                //    Description = m.Description,
                //    TargetDate = m.TargetDate
                //}).ToList() ?? new List<Milestone>(),

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };



            await _repo.AddAsync(data);
            return data;
        }

        public async Task DeleteIdeaAsync(string id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<IEnumerable<BusinessIdeas>> GetAllIdeasAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<BusinessIdeas> GetByIdAsync(string id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<BusinessIdeas>> GetByCreatorAsync(string creatorId)
        {
            return await _repo.GetByCreatorIdAsync(creatorId);
        }

        public async Task<IEnumerable<BusinessIdeas>> GetPendingIdeasAsync()
        {
            return await _repo.GetPendingIdeasAsync();
        }
        //public async Task<BusinessIdeas> UpdateIdeaAsync(UpdateIdeaDto dto, string userId)
        //{
        //    var idea = await _repo.GetByIdAsync(dto.Id);
        //    if (idea == null)
        //    {
        //        return null;
        //    }
        //    if (idea.CreatorId != userId) return null;
        //    if (idea.Status is "Approved" or "Submitted" or "Rejected")
        //        return Result.Failure<BusinessIdeas>("Cannot edit in current status");

        //    // ম্যাপ করা (যেগুলো পাঠানো হয়েছে শুধু সেগুলো আপডেট)
        //    if (dto.CompanyName != null) idea.CompanyName = dto.CompanyName;
        //    if (dto.FounderIdentity != null) idea.FounderIdentity = dto.FounderIdentity;
        //    // ... একইভাবে বাকি ফিল্ড

        //    idea.UpdatedAt = DateTime.UtcNow;

        //    await _repository.ReplaceAsync(idea);
        //    return Result.Success(idea);
        //}
        //public async Task<BusinessIdeas> UpdateIdeaAsync(
        // BusinessIdeas existingIdea,
        // CreateIdeaDto request)
        //{
        //    existingIdea.Title = request.Title ?? existingIdea.Title;
        //    existingIdea.Summary = request.Summary ?? existingIdea.Summary;
        //    existingIdea.MarketSize = request.MarketSize ?? existingIdea.MarketSize;
        //    existingIdea.Problem = request.Problem ?? existingIdea.Problem;
        //    existingIdea.Solution = request.Solution ?? existingIdea.Solution;
        //    existingIdea.RevenueModel = request.RevenueModel ?? existingIdea.RevenueModel;

        //    if (!string.IsNullOrEmpty(request.Stage))
        //        existingIdea.Stage = request.Stage;

        //    if (request.FundingRequired > 0)
        //        existingIdea.FundingRequired = request.FundingRequired;

        //    if (request.EquityOffered > 0)
        //        existingIdea.EquityOffered = request.EquityOffered;

        //    // Replace milestones (MongoDB-friendly)
        //    if (request.Milestones != null)
        //    {
        //        existingIdea.Milestones = request.Milestones.Select(m => new Milestone
        //        {
        //            Title = m.Title,
        //            Description = m.Description,
        //            TargetDate = m.TargetDate,
        //            Status = "Pending"
        //        }).ToList();
        //    }

        //    await _repo.UpdatePartialAsync(existingIdea.Id, existingIdea);
        //    return existingIdea;
        //}



    }
}
