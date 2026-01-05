using WebApp.Services.Interface;

namespace WebApp.Services
{
    public class FcmPushService : IPushService
    {
        public async Task Send(Guid userId, string title, string body)
        {
            // Implement  Web Push here
            await Task.CompletedTask;
        }
    }
}
