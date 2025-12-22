using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessIdeaController : ControllerBase
    {

        private readonly IBusinessIdeasService _service;
        public BusinessIdeaController(IBusinessIdeasService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(BusinessIdeas idea)
        {
            var result = await _service.CreateIdeaAsync(idea);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var idea = await _service.GetByIdAsync(id);
            if (idea == null) return NotFound();
            return Ok(idea);
        }

        [HttpGet("creator/{creatorId}")]
        public async Task<IActionResult> GetByCreator(string creatorId)
        {
            var ideas = await _service.GetByCreatorAsync(creatorId);
            return Ok(ideas);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingIdeas()
        {
            var ideas = await _service.GetPendingIdeasAsync();
            return Ok(ideas);
        }

        //[HttpPut("{id}")]
        //public async Task<IActionResult> Update(string id, BusinessIdeas idea)
        //{
        //    //idea.Id = id;
        //    var updated = await _service.UpdateIdeaAsync(id, idea);
        //    return Ok(updated);
        //}

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteIdeaAsync(id);
            return NoContent();
        }



    }
}
