using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByUserIdAsync(string userId);
        Task CreateAsync(Company company);
    }
}
