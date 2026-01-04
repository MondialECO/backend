using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvestorController : ControllerBase
    {
        private readonly IInvestmentsService _service;
        public InvestorController(IInvestmentsService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(Investments investment)
        {
            var result = await _service.CreateInvestmentAsync(investment);
            return Ok(result);
        }

        [HttpGet("investor/{investorId}")]
        public async Task<IActionResult> GetByInvestor(string investorId)
        {
            var investments = await _service.GetByInvestorAsync(investorId);
            return Ok(investments);
        }

        [HttpGet("idea/{ideaId}")]
        public async Task<IActionResult> GetByIdea(string ideaId)
        {
            var investments = await _service.GetByIdeaAsync(ideaId);
            return Ok(investments);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteInvestmentAsync(id);
            return NoContent();
        }

    }
}
