using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirst("sub")?.Value;
            return Ok(await _service.GetByUser(userId));
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkRead(string id)
        {
            await _service.MarkRead(id);
            return NoContent();
        }
    }
}
