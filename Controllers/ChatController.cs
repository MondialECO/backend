using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using WebApp.Models.DatabaseModels;
using WebApp.Models.Dtos;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hub;

        public ChatController(
            IChatService chatRepo,
            IHubContext<ChatHub> hub)
        {
            _chatService = chatRepo;
            _hub = hub;
        }

        // Get current user ID from JWT token
        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException());

        // Conversation list
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var data = await _chatService.GetUserConversations(CurrentUserId);
            return Ok(data);
        }

        // Message history
        [HttpGet("messages/{conversationId}")]
        public async Task<IActionResult> GetMessages(
            string conversationId,
            int skip = 0,
            int limit = 30)
        {
            var messages = await _chatService.GetMessages(
                ObjectId.Parse(conversationId), skip, limit);

            return Ok(messages);
        }

        // 3️⃣ Send message (REST fallback)
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage(SendMessageRequest request)
        {
            var message = new ChatMessage
            {
                ConversationId = ObjectId.Parse(request.ConversationId),
                SenderId = CurrentUserId,
                Message = request.Message
            };

           var data = await _chatService.AddMessage(message);

            // Try realtime delivery
            await _hub.Clients.Group(request.ConversationId)
                .SendAsync("ReceiveMessage", data);

            return Ok(data);
        }

        // Mark as read
        [HttpPost("read/{conversationId}")]
        public async Task<IActionResult> MarkRead(string conversationId)
        {
            await _chatService.MarkAsRead(
                ObjectId.Parse(conversationId),
                CurrentUserId
            );

            return Ok();
        }



    }
}
