namespace WebApp.Services.Interface
{
    public interface IPushService
    {
        Task Send(Guid userId, string title, string body);
    }
}
