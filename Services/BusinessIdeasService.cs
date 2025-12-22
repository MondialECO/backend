using Org.BouncyCastle.Asn1.Ocsp;
using WebApp.Models.BusinessIdeas;
using WebApp.Models.DatabaseModels;
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

        public async Task<BusinessIdeas> CreateIdeaAsync(BusinessIdeas idea)
        {
            await _repo.AddAsync(idea);
            return idea;
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

        public async Task<BusinessIdeas> UpdateIdeaAsync(
         BusinessIdeas existingIdea,
         CreateIdeaDto request)
        {
            existingIdea.Title = request.Title ?? existingIdea.Title;
            existingIdea.Summary = request.Summary ?? existingIdea.Summary;
            existingIdea.MarketSize = request.MarketSize ?? existingIdea.MarketSize;
            existingIdea.Problem = request.Problem ?? existingIdea.Problem;
            existingIdea.Solution = request.Solution ?? existingIdea.Solution;
            existingIdea.RevenueModel = request.RevenueModel ?? existingIdea.RevenueModel;

            if (!string.IsNullOrEmpty(request.Stage))
                existingIdea.Stage = request.Stage;

            if (request.FundingRequired > 0)
                existingIdea.FundingRequired = request.FundingRequired;

            if (request.EquityOffered > 0)
                existingIdea.EquityOffered = request.EquityOffered;

            // Replace milestones (MongoDB-friendly)
            if (request.Milestones != null)
            {
                existingIdea.Milestones = request.Milestones.Select(m => new Milestone
                {
                    Title = m.Title,
                    Description = m.Description,
                    TargetDate = m.TargetDate,
                    Status = "Pending"
                }).ToList();
            }

            await _repo.UpdatePartialAsync(existingIdea.Id, existingIdea);
            return existingIdea;
        }



    }
}
