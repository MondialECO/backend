using WebApp.Models.BusinessIdeas;
using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface IBusinessIdeasService
    {
        Task<BusinessIdeas> CreateIdeaAsync(BusinessIdeas idea);
        Task<IEnumerable<BusinessIdeas>> GetAllIdeasAsync();
        Task<IEnumerable<BusinessIdeas>> GetByCreatorAsync(string creatorId);
        Task<BusinessIdeas> GetByIdAsync(string id);
        Task<BusinessIdeas> UpdateIdeaAsync(BusinessIdeas existingIdea, CreateIdeaDto request);
        Task DeleteIdeaAsync(string id);
        Task<IEnumerable<BusinessIdeas>> GetPendingIdeasAsync();
    }
}
