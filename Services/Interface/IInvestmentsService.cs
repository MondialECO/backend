using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface IInvestmentsService
    {
        Task<Investments> CreateInvestmentAsync(Investments investment);
        Task<IEnumerable<Investments>> GetByInvestorAsync(string investorId);
        Task<IEnumerable<Investments>> GetByIdeaAsync(string ideaId);
        Task<IEnumerable<Investments>> GetByIdeaIdsAsync(List<string> ideaIds);

        Task<Investments> GetByIdAsync(string id);
        Task DeleteInvestmentAsync(string id);

    }
}
