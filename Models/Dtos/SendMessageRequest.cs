namespace WebApp.Models.Dtos
{
    public class SendMessageRequest
    {
        public string ConversationId { get; set; } = "";
        public string Message { get; set; } = "";
    }

}
