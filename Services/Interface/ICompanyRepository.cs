using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface ICompanyRepository
    {
        Task<companies?> GetByUserIdAsync(string userId);
        Task CreateAsync(companies company);
    }
}
