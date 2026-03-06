using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Services
{
    public class TransactionsService : ITransactionsService
    {
        private readonly TransactionsRepository _repo;

        public TransactionsService(TransactionsRepository repo)
        {
            _repo = repo;
        }


        public async Task<Transactions> CreateTransactionAsync(Transactions transaction)
        {
            await _repo.AddAsync(transaction);
            return transaction;
        }


        public async Task DeleteTransactionAsync(string id)
        {
            await _repo.DeleteAsync(id);
        }


        public async Task<Transactions> GetByIdAsync(string id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Transactions>> GetByUserAsync(string userId)
        {
            return await _repo.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Transactions>> GetRecentAsync(int limit = 50)
        {
            return await _repo.GetRecentAsync(limit);
        }
    }
}
