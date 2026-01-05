using MongoDB.Bson;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Services
{
    public class ChatService : IChatService
    {
        private readonly MessagesRepository _messages;
        private readonly ConversationRepository _conversation;
        public ChatService(MessagesRepository messages, ConversationRepository conversation)
        {
            _messages = messages;
            _conversation = conversation;
        }

        public async Task<ChatMessage> AddMessage(ChatMessage message)
        {
            await _messages.AddMessage(message);
            await _conversation.UpdateConversionLastMessage(message);
            return message;
        }

        public async Task<List<ChatMessage>> GetMessages(ObjectId conversationId, int skip, int limit)
        {
           return await _messages.GetMessages(conversationId, skip, limit);
        }

        public async Task<Conversation> GetOrCreateConversation(Guid user1, Guid user2)
        {
            return await _conversation.GetOrCreateConversation(user1, user2);
        }

        public async Task<List<Conversation>> GetUserConversations(Guid userId)
        {
            return await _conversation.GetUserConversations(userId);
        }

        public async Task MarkAsRead(ObjectId conversationId, Guid userId)
        {
           await _messages.MarkAsRead(conversationId, userId);
        }

    }
}
