using WebApp.Models.DatabaseModels;
using WebApp.Models.Dtos;

namespace WebApp.Services.Interface
{
    public interface IBusinessIdeasService
    {
        Task<BusinessIdeas> CreateIdeaAsync(CreateIdeaDto idea, string userid, List<string> mediaPaths, List<string> documentPaths);
        Task<IEnumerable<BusinessIdeas>> GetAllIdeasAsync();
        Task<IEnumerable<BusinessIdeas>> GetByCreatorAsync(string creatorId);
        Task<BusinessIdeas> GetByIdAsync(string id);
        Task<BusinessIdeas> UpdateIdeaAsync(CreateIdeaDto dto, string userid, string id, List<string> mediaPaths, List<string> documentPaths);
        Task DeleteIdeaAsync(string id);
        Task<IEnumerable<BusinessIdeas>> GetPendingIdeasAsync();
    }
}
