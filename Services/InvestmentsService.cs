using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Services
{
    public class InvestmentsService : IInvestmentsService
    {
        private readonly InvestmentsRepository _repo;

        public InvestmentsService(InvestmentsRepository repo)
        {
            _repo = repo;
        }

        public async Task<Investments> CreateInvestmentAsync(Investments investment)
        {
            await _repo.AddAsync(investment);
            return investment;
        }

        public async Task DeleteInvestmentAsync(string id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<IEnumerable<Investments>> GetByIdeaAsync(string ideaId)
        {
            return await _repo.GetByIdeaIdAsync(ideaId);
        }
        public async Task<IEnumerable<Investments>> GetByIdeaIdsAsync(List<string> ideaIds)
        {
            return await _repo.GetByIdeaIdsAsync(ideaIds);
        }

        public async Task<IEnumerable<Investments>> GetByInvestorAsync(Guid investorId)
        {
            return await _repo.GetByInvestorIdAsync(investorId);
        }

        public async Task<Investments> GetByIdAsync(string id)
        {
            return await _repo.GetByIdAsync(id);
        }
    }
}
