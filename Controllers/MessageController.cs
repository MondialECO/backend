using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Dtos;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _service;

        public MessageController(IMessageService service)
        {
            _service = service;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = User.FindFirst("sub")?.Value;
            return Ok(await _service.GetConversations(userId));
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(SendMessageDto dto)
        {
            var senderId = User.FindFirst("sub")?.Value;
            await _service.SendMessage(dto, senderId);
            return Ok();
        }
    }
}
