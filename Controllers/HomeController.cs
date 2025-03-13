using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.InterfaceRepository;
using WebApp.Models;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using WebApp.Services.Repository;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IInfoRepository _infoRepository;
        private readonly IFAQsRepository _faqRepository;
        private readonly ITestimonialRepository _testimonialRepository;

        public HomeController(IInfoRepository infoRepository,
            IFAQsRepository faqRepository,
            ITestimonialRepository testimonialRepository)
        {
            _infoRepository = infoRepository;
            _faqRepository = faqRepository;
            _testimonialRepository = testimonialRepository;
        }







        // Get: api/home/contact
        [HttpGet("about")]
        public async Task<IActionResult> About()
        {
            string id = "7b4a446c-0ddf-4538-8d19-7a7bd9e6d0f8";
            var about = await _infoRepository.GetAboutByIdAsync(id);
            if (about == null)
            {
                return NotFound(new { Message = "About info not found." });
            }
            return Ok(about);
        }

        // Get: api/home/contact
        [HttpGet("contact")]
        public async Task<IActionResult> Contact()
        {
            string id = "cb7a4b9e-d238-456e-882b-734fc21db4f0";
            var info = await _infoRepository.GetContactByIdAsync(id);
            if (info == null)
            {
                return NotFound(new { Message = "Contact info not found." });
            }
            return Ok(info);
        }

        [HttpGet("FAQ-List")]
        public async Task<IActionResult> FAQsList()
        {
            var faqs = await _faqRepository.GetAllAsync();
            if (faqs == null || !faqs.Any())
            {
                return NotFound(new { Message = "No FAQs found." });
            }
            return Ok(faqs);
        }

        // GET: api/Home/best-testimonials
        [HttpGet("best-testimonials")]
        public async Task<ActionResult<IEnumerable<TestimonialModel>>> GetBestTestimonials()
        {
            var bestTestimonials = await _testimonialRepository.GetBestAsync();
            return Ok(bestTestimonials);
        }


        // POST: api/Admin/create-testimonial
        [HttpPost("create-testimonial")]
        public async Task<ActionResult> CreateTestimonial(TestimonialUserModel model)
        {
            string imagePath = null;
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.imageFile != null && model.imageFile.Length > 0)
            {
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "Testimonial");

                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.imageFile.FileName)}";
                var filePath = Path.Combine(imagesPath, fileName);

                try
                {

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.imageFile.CopyToAsync(stream);
                    }

                    imagePath = $"/images/Testimonial/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "An error occurred while uploading the image.", Error = ex.Message });
                }
            }

            // Create TestimonialModel to store in MongoDB
            var testimonial = new TestimonialModel
            {
                Id = Guid.NewGuid().ToString(),
                CompanyName = model.CompanyName,
                Description = model.Description,
                Profession = model.Profession,
                Point = model.Point,
                Author = model.Author,
                Image = imagePath
            };

            await _testimonialRepository.AddOneAsync(testimonial);
            return Ok(new { Message = "Testimonial Created successfully." });
        }
    }
}
