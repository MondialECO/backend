using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface IUserLogsRepository
    {
        Task<List<ActivityLogsModel>> GetAllAsync();
        Task<List<ActivityLogsModel>> GetByIdAsync(string Id);
        Task<ActivityLogsModel> GetByIpAsync(string ipAddress);
        Task AddAsync(ActivityLogsModel activityLogs);
        Task UpdateAsync(ActivityLogsModel activityLogs);
        Task DeleteAsync(string id);
        Task<ActivityLogsModel> GetByUserNameAsync(string userName);
        Task<ActivityLogsModel> GetByUserNameAndIpAddressAsync(string userName, string ipAddress);
    }
}
