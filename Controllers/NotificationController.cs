using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirst("sub")!.Value);

        [HttpGet]
        public async Task<IActionResult> GetNotifications(int skip = 0, int limit = 30)
        {
            var data = await _service.GetUserNotifications(CurrentUserId, skip, limit);
            return Ok(data);
        }

        [HttpPost("read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            await _service.MarkAsRead(ObjectId.Parse(id));
            return Ok();
        }
    }
}
