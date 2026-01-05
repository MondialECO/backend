using Amazon.Runtime.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using Org.BouncyCastle.Asn1.Ocsp;
using System.IdentityModel.Tokens.Jwt;
using WebApp.Models.DatabaseModels;
using WebApp.Models.Dtos;
using WebApp.Services.Interface;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task JoinConversation(string conversationId)
    {
        if (!ObjectId.TryParse(conversationId, out _))
            throw new HubException("Invalid conversation id");

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new HubException("Message empty");

        var senderId = Guid.Parse(
            Context.User!.FindFirst(JwtRegisteredClaimNames.Sub)!.Value
        );
        var message = new ChatMessage
        {
            ConversationId = ObjectId.Parse(request.ConversationId),
            SenderId = senderId,
            Message = request.Message
        };
        var savedMessage = await _chatService.AddMessage(message);

        await Clients.Group(request.ConversationId)
            .SendAsync("ReceiveMessage", savedMessage);
    }
}
