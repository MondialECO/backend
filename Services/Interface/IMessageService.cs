using WebApp.Models.Dtos;

namespace WebApp.Services.Interface
{
    public interface IMessageService
    {
        Task<IEnumerable<ConversationDto>> GetConversations(string userId);
        Task SendMessage(SendMessageDto dto, string senderId);
    }
}
