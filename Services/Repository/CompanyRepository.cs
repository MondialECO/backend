using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;

namespace WebApp.Services.Repository
{
    public class CompanyRepository : ICompanyRepository<Company>
    {
        public Task CreateAsync(Company company)
        {
            throw new NotImplementedException();
        }

        public Task<Company?> GetByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }

    public interface ICompanyRepository<T>
    {
    }
}
