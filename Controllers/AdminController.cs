using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using Microsoft.IdentityModel.Tokens;
using WebApp.Models;
using WebApp.DbContext;
using MongoDB.Driver;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly MongoDbContext _context;
        public AdminController(UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
             MongoDbContext context

            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _userManager.Users.Select(user => new
            {
                user.Id,
                user.Name,
                user.Email,
                user.PhoneNumber,
                user.User,
                user.LockoutEnd,
                user.CreatedOn,
                user.Address
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
                user.Name,
                user.Email,
                user.PhoneNumber,
                user.UserName,
                user.Address.address,
                user.Address.City,
                user.Address.Country,
                user.LockoutEnd,
                user.CreatedOn
            });
        }

        // POST: api/admin/create-role
        //[HttpPost("create-role")]
        //public async Task<IActionResult> CreateRole(string roleName, string description)
        //{
        //    if (await _roleManager.RoleExistsAsync(roleName))
        //        return BadRequest(new { Message = "Role already exists" });

        //    var role = new ApplicationRole
        //    {
        //        Name = roleName,
        //        Description = description
        //    };

        //    var result = await _roleManager.CreateAsync(role);
        //    if (result.Succeeded)
        //        return Ok(new { Message = "Role created successfully" });

        //    return BadRequest(result.Errors);
        //}


        // POST: api/admin/assign-role
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRoleToUser(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (!await _roleManager.RoleExistsAsync(roleName))
                return NotFound(new { Message = "Role not found" });

            var existingRoles = await _userManager.GetRolesAsync(user);

            // --- Remove old roles ---
            if (existingRoles.Any())
            {
                var remove = await _userManager.RemoveFromRolesAsync(user, existingRoles);
                if (!remove.Succeeded)
                    return BadRequest(remove.Errors);
            }

            // --- Assign new role ---
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // --- Update custom field (optional) ---
            user.User = roleName;
            await _userManager.UpdateAsync(user);

            return Ok(new { Message = "Role assigned successfully" });
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


    }

}