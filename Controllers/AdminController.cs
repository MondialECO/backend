using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using Microsoft.IdentityModel.Tokens;
using WebApp.Models;
using WebApp.InterfaceRepository;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IInfoRepository _infoRepository;
        private readonly IFAQsRepository _faqRepository;
        private readonly ITestimonialRepository _testimonialRepository;
        public AdminController(UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IInfoRepository infoRepository,
            IFAQsRepository faqRepository,
            ITestimonialRepository testimonialRepository
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _infoRepository = infoRepository;
            _faqRepository = faqRepository;
            _testimonialRepository = testimonialRepository;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _userManager.Users.Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.LockoutEnd
            }).ToList();

            return Ok(users);
        }

        // GET: api/admin/user/{id}
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.UserName,
                user.Address.address,
                user.Address.City,
                user.Address.Country,
                user.Roles,
                user.LockoutEnd,
                user.CreatedOn
            });
        }

        // POST: api/admin/create-role
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole(string roleName, string description)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                return BadRequest(new { Message = "Role already exists" });

            var role = new ApplicationRole
            {
                Name = roleName,
                Description = description
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
                return Ok(new { Message = "Role created successfully" });

            return BadRequest(result.Errors);
        }


        // POST: api/admin/assign-role
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRoleToUser(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (!await _roleManager.RoleExistsAsync(roleName))
                return NotFound(new { Message = "Role not found" });

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
                return Ok(new { Message = "Role assigned successfully" });

            return BadRequest(result.Errors);
        }


        // DELETE: api/admin/delete-user/{id}
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "User deleted successfully" });
        }

        // POST: api/admin/disable-login
        [HttpPost("disable-login")]
        public async Task<IActionResult> DisableLogin([FromBody] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            user.LockoutEnd = DateTimeOffset.MaxValue;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = $"User '{user.UserName}' login has been disabled." });
            }

            return BadRequest(new { Message = $"Failed to disable login for user '{user.UserName}'." });
        }

        // POST: api/admin/enable-login
        [HttpPost("enable-login")]
        public async Task<IActionResult> EnableLogin([FromBody] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            user.LockoutEnd = null;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = $"User '{user.UserName}' login has been enabled." });
            }

            return BadRequest(new { Message = $"Failed to enable login for user '{user.UserName}'." });
        }

        //// Get: api/admin/contact
        //[HttpGet("contact")]
        //public async Task<IActionResult> Contact()
        //{
        //    string id = "cb7a4b9e-d238-456e-882b-734fc21db4f0";
        //    var info = await _infoRepository.GetContactByIdAsync(id);
        //    if (info == null)
        //    {
        //        return NotFound(new { Message = "Contact info not found." });
        //    }
        //    return Ok(info);
        //}

        [HttpPut("contact-update")]
        public async Task<IActionResult> ContactUpdate(ContactModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Id))
            {
                return BadRequest(new { Message = "Invalid request data." });
            }

            var existingContact = await _infoRepository.GetContactByIdAsync(model.Id);
            if (existingContact != null)
            {
                existingContact.Contact_Message = model.Contact_Message ?? existingContact.Contact_Message;
                existingContact.Emergency_Hotline = model.Emergency_Hotline ?? existingContact.Emergency_Hotline;
                existingContact.General_Phone = model.General_Phone ?? existingContact.General_Phone;
                existingContact.General_Email = model.General_Email ?? existingContact.General_Email;
                existingContact.Sales_Phone = model.Sales_Phone ?? existingContact.Sales_Phone;
                existingContact.Sales_Email = model.Sales_Email ?? existingContact.Sales_Email;
                existingContact.Address = model.Address ?? existingContact.Address;
                existingContact.MapLink = model.MapLink ?? existingContact.MapLink;
                existingContact.Facebook_link = model.Facebook_link ?? existingContact.Facebook_link;
                existingContact.Linkdin_Link = model.Linkdin_Link ?? existingContact.Linkdin_Link;
                existingContact.Youtube_link = model.Youtube_link ?? existingContact.Youtube_link;
                existingContact.Whatsapp_link = model.Whatsapp_link ?? existingContact.Whatsapp_link;
                existingContact.Instragram_link = model.Instragram_link ?? existingContact.Instragram_link;
                existingContact.Tiktok_link = model.Tiktok_link ?? existingContact.Tiktok_link;
                existingContact.X_link = model.X_link ?? existingContact.X_link;
                await _infoRepository.UpdateContactAsync(model.Id, model);
                return Ok(new { Message = "Contact updated successfully." });
            }

            else
            {
                return StatusCode(500, new { Message = "Failed to update contact." });
            }
        }


        public class aboutImage
        {
            public IFormFile Img { get; set; }
        }

        [HttpPut("about-update")]
        public async Task<IActionResult> AboutUpdate(AboutModel model, aboutImage Img)
        {
            model.Id = "7b4a446c-0ddf-4538-8d19-7a7bd9e6d0f8";

            if (string.IsNullOrEmpty(model.Id))
            {
                return BadRequest(new { Message = "Invalid request data." });
            }

            if (Img?.Img != null)
            {
                var imagesPath = Path.Combine("wwwroot", "images", "index");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                var fileExtension = Path.GetExtension(Img.Img.FileName).ToLower();
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                if (!validExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { Message = "Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed." });
                }

                var fileName = Img.Img.FileName;
                var filePath = Path.Combine(imagesPath, fileName);

                try
                {
                    if (!string.IsNullOrEmpty(model.Image))
                    {
                        var oldFilePath = Path.Combine("wwwroot", model.Image.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Img.Img.CopyToAsync(stream);
                    }

                    model.Image = $"/images/index/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "An error occurred while uploading the image.", Error = ex.Message });
                }
            }

            await _infoRepository.UpdateAboutAsync(model.Id, model);

            return Ok(new { Message = "About information created successfully." });
        }



        [HttpPost("create-FAQ")]
        public async Task<IActionResult> CreateFAQ(FAQsModel model)
        {
            model.Id = Guid.NewGuid().ToString();
            if (model == null || string.IsNullOrEmpty(model.Id))
            {
                return BadRequest(new { Message = "Invalid request data. Question and Answer are required." });
            }
            try
            {
                await _faqRepository.AddAsync(model);
                return Ok(new { Message = "FAQ created successfully.", Data = model });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating the FAQ.", Error = ex.Message });
            }
        }

        [HttpPut("update-FAQ/{id}")]
        public async Task<IActionResult> UpdateFAQ(string id, FAQsModel model)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { Message = "FAQ ID is required." });
            }

            var faq = await _faqRepository.GetByIdAsync(id);
            if (faq == null)
            {
                return NotFound(new { Message = "FAQ Not Found" });
            }

            try
            {
                // Only update properties if they are provided
                faq.type = !string.IsNullOrEmpty(model.type) ? model.type : faq.type;
                faq.Question = !string.IsNullOrEmpty(model.Question) ? model.Question : faq.Question;
                faq.Answer = !string.IsNullOrEmpty(model.Answer) ? model.Answer : faq.Answer;

                await _faqRepository.UpdateAsync(id, faq);
                return Ok(new { Message = "FAQ updated successfully.", Data = faq });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the FAQ.", Error = ex.Message });
            }
        }

        [HttpDelete("delete-FAQ/{id}")]
        public async Task<IActionResult> DeleteFAQ(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { Message = "FAQ ID is required." });
            }

            var faq = await _faqRepository.GetByIdAsync(id);
            if (faq == null)
            {
                return NotFound(new { Message = "FAQ Not Found" });
            }

            try
            {
                await _faqRepository.DeleteAsync(id);
                return Ok(new { Message = "FAQ deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the FAQ.", Error = ex.Message });
            }
        }

        // GET: api/Admin/testimonial-list
        [HttpGet("testimonial-list")]
        public async Task<ActionResult<IEnumerable<TestimonialModel>>> GetAllTestimonials()
        {
            var testimonials = await _testimonialRepository.GetAllAsync();
            return Ok(testimonials);
        }

        // GET: api/Admin/get-testimonial/{id}
        [HttpGet("testimonial/{id}")]
        public async Task<ActionResult<TestimonialModel>> GetTestimonialById(string id)
        {
            var testimonial = await _testimonialRepository.GetByIdAsync(id);
            if (testimonial == null)
                return NotFound(new { message = "Testimonial not found" });

            return Ok(testimonial);
        }


        // PUT: api/Admin/testimonial/{id}
        [HttpPut("testimonial/{id}")]
        public async Task<ActionResult> UpdateTestimonial(string id, TestimonialUserModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingTestimonial = await _testimonialRepository.GetByIdAsync(id);
            if (existingTestimonial == null)
                return NotFound(new { message = "Testimonial not found" });

            string imagePath = null;
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

                    if (!string.IsNullOrEmpty(existingTestimonial.Image))
                    {
                        var oldFilePath = Path.Combine("wwwroot", existingTestimonial.Image.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    imagePath = $"/images/Testimonial/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "An error occurred while uploading the image.", Error = ex.Message });
                }
            }

            try
            {
                var testimonial = new TestimonialModel
                {
                    Id = id,
                    CompanyName = model.CompanyName,
                    Description = model.Description,
                    Profession = model.Profession,
                    Point = model.Point,
                    Author = model.Author,
                    Image = imagePath
                };

                await _testimonialRepository.UpdateAsync(id, testimonial);
                return Ok(new { Message = "Testimonial updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while Update the Testimonial.", Error = ex.Message });
            }

        }

        // DELETE: api/Admin/testimonial/{id}
        [HttpDelete("testimonial/{id}")]
        public async Task<ActionResult> DeleteTestimonial(string id)
        {
            var existingTestimonial = await _testimonialRepository.GetByIdAsync(id);
            if (existingTestimonial == null)
                return NotFound(new { message = "Testimonial not found" });

            try
            {
                await _testimonialRepository.DeleteAsync(id);
                return Ok(new { Message = "Testimonial Deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while Delete the Testimonial.", Error = ex.Message });
            }
        }


    }

}