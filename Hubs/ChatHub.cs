using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;

[Authorize]
public class ChatHub : Hub
{
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task SendMessage(string conversationId, string message)
    {
        var senderId = Context.User?
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        });
    }
}

