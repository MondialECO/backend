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
        public async Task<BusinessIdeas> CreateIdeaAsync(CreateIdeaDto idea, string userid)
        {
            var data = new BusinessIdeas
            {
                CreatorId = userid,
                Name = idea.Name,
                Problem = idea.Problem,
                Solution = idea.Solution,
                Market = idea.Market,
                BusinessModel = idea.BusinessModel,
                Operations = idea.Operations,
                Roadmap = idea.Roadmap,
                Compliance = idea.Compliance,
                FounderIdentity = idea.FounderIdentity,
                ImageVideo = idea.ImageVideoUrls,
                DocumentUrls = idea.DocumentUrls,
                Status = idea.Status ?? "Draft", // Draft | Submitted | Approved | Rejected
                FundingRequired = idea.FundingRequired ?? 0,
                EquityOffered = idea.EquityOffered ?? 0,

                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(data);
            return data;
        }

        // Update idea 
        public async Task<BusinessIdeas> UpdateIdeaAsync(CreateIdeaDto idea, string userid, string id)
        {
            var existingIdeas = await _repo.GetByIdeaDriftAsync(id, userid);

            var data = new BusinessIdeas
            {
                CreatorId = existingIdeas.CreatorId,
                Name =idea.Name ?? existingIdeas.Name,
                FounderIdentity = idea.FounderIdentity ?? existingIdeas.FounderIdentity,
                Problem = idea.Problem ?? existingIdeas.Problem,
                Solution = idea.Solution ?? existingIdeas.Solution,
                Market = idea.Market ?? existingIdeas.Market,
                BusinessModel = idea.BusinessModel ?? existingIdeas.BusinessModel,
                Operations = idea.Operations ?? existingIdeas.Operations,
                Roadmap = idea.Roadmap ?? existingIdeas.Roadmap,
                Compliance = idea.Compliance ?? existingIdeas.Compliance,
                IsPublished = existingIdeas.IsPublished,
                FundingRequired = idea.FundingRequired ?? existingIdeas.FundingRequired,
                EquityOffered = idea.EquityOffered ?? existingIdeas.EquityOffered,
                Impressions = existingIdeas.Impressions,
                Clicks = existingIdeas.Clicks,
                Status = idea.Status, // Draft | Submitted | Approved | Rejected
                ImageVideo = idea.ImageVideoUrls ?? existingIdeas.ImageVideo,
                DocumentUrls = idea.DocumentUrls ?? existingIdeas.DocumentUrls,

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
