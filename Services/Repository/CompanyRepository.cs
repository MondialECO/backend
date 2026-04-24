using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;

namespace WebApp.Services.Repository
{
    public class CompanyRepository : ICompanyRepository<companies>
    {
        public Task CreateAsync(companies company)
        {
            throw new NotImplementedException();
        }

        public Task<companies?> GetByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }

    public interface ICompanyRepository<T>
    {
    }
}
