using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using WebApp.DbContext;
using WebApp.Middleware;
using WebApp.Models;
using WebApp.Models.DatabaseModels;
using static Org.BouncyCastle.Math.EC.ECCurve;
namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;


        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            RoleManager<ApplicationRole> roleManager,
            EmailService emailSender,
            ILogger<AuthController> logger

            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _emailService = emailSender;
            _logger = logger;
        }


        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);


                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return Unauthorized(new { Message = "Invalid email or password" });

                if (!user.EmailConfirmed)
                {
                    return Unauthorized(new { Message = "You need to confirm your email before logging in." });
                }

                if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                    return Unauthorized(new { Message = "Invalid email or password" });

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                    return Unauthorized(new { Message = "Your account is locked. Please try again later." });

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.Count > 0 ? roles[0] : "User";

                var token = JwtTokenHelper.GenerateToken(user.Id.ToString(), role, _configuration["JwtSettings:Key"], _configuration["JwtSettings:Issuer"], _configuration["JwtSettings:Audience"]);
                _logger.LogInformation($"User {user.Email} successfully logged in.");

                var refreshToken = JwtTokenHelper.GenerateRefreshToken();

                var refreshTokenEntity = new RefreshToken
                {
                    
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                };

                user.RefreshToken = refreshTokenEntity;
                user.LastLogin = DateTime.UtcNow;
                // Here you would typically save the refresh token to your database
                var updateResult = await _userManager.UpdateAsync(user);

                //await _signInManager.SignInAsync(user, user.LastLogin == refreshTokenEntity.CreatedAt);
                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Roles = roles


                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }

        public class TokenRefreshRequest
        {
            public string AccessToken { get; set; } = "";
            public string RefreshToken { get; set; } = "";
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenRefreshRequest request)
        {
            var principal = JwtTokenHelper.GetPrincipalFromExpiredToken(
                request.AccessToken,
               _configuration["JwtSettings:Key"],
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"]
            );

            var userId = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (userId == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized();

            // Validate refresh token
            if (user.RefreshToken == null ||
                user.RefreshToken.Token != request.RefreshToken ||
                user.RefreshToken.ExpiresAt < DateTime.UtcNow ||
                user.RefreshToken.IsRevoked)
            {
                return Unauthorized("Invalid refresh token");
            }

            // Generate new tokens
            var newAccessToken = JwtTokenHelper.GenerateToken(
                user.Id.ToString(),
                user.User,
                _configuration["JwtSettings:Key"],
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"]
            );

            var newRefreshToken = JwtTokenHelper.GenerateRefreshToken();

            // Rotate refresh token
            user.RefreshToken.Token = newRefreshToken;
            user.RefreshToken.ExpiresAt = DateTime.UtcNow.AddDays(7);

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Ok();

            user.RefreshToken.IsRevoked = true;
            await _userManager.UpdateAsync(user);
            await _signInManager.SignOutAsync();
            return Ok("Logged out");
        }




        //[HttpPost("logout")]
        //public async Task<IActionResult> Logout()
        //{
        //    await _signInManager.SignOutAsync();
        //    return Ok(new { Message = "Logged out successfully." });
        //}



        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid || model.User == "Admin" || model.User == "admin")
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { Message = "Email already in use" });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                User = model.User,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { Message = "User registration failed", Errors = result.Errors });

            var roleExists = await _roleManager.RoleExistsAsync(model.User);
            if (!roleExists)
            {
                return BadRequest(new { Message = "Failed to create default role" });
            }

            await _userManager.AddToRoleAsync(user, model.User);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedToken = WebUtility.UrlEncode(token);

            var confirmationLink = $"https://mondialbusiness.eu/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&token={encodedToken}";

            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirm Email</a>");

            if (!emailSent)
                return StatusCode(500, new { Message = "User registered, but failed to send confirmation email." });

            return Ok(new { Message = "User registered successfully! Please check your email for confirmation." });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
        {
            if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.Token))
            {
                return BadRequest(new { Message = "Invalid confirmation request. User ID and Token are required." });
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new { Message = "Your email is already confirmed. You can log in now." });
            }
            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Email confirmation failed. Invalid or expired token." });
            }

            return Ok(new { Message = "Email confirmed successfully! You can now log in." });
        }


        public class ForgotPasswordModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid email"
                });
            }

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { Message = "User not found" });

            // Generate the reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            // var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={encodedToken}&email={model.Email}";
            var resetUrl = $"https://mondialbusiness.eu/reset-password?email={model.Email}&token={encodedToken}";

            _logger.LogInformation($"Generated password reset token for {model.Email}");
            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Password Reset",
                $"Click here to reset your password: <a href='{resetUrl}'>Reset Password</a>");

            if (!emailSent)
                return StatusCode(500, new { Message = "Failed to send reset password email." });

            return Ok(new { Message = "Password reset link sent to your email." });
        }


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { Message = "User not found" });

            // Reset the password using the token
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { Message = "Invalid or expired token." });

            _logger.LogInformation($"Password successfully reset for {model.Email}");
            return Ok(new { Message = "Password reset successfully." });
        }

        public class ChangePasswordModel
        {
            public string currentPassword { get; set; }
            public string newPassword { get; set; }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated." });

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Message = "User not found." });

                var result = await _userManager.ChangePasswordAsync(user, model.currentPassword, model.newPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        Message = "Password change failed.",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }

                _logger.LogInformation($"Password successfully reset for user {user.Email}");

                return Ok(new { Message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpGet("account")]
        public async Task<IActionResult> GetAccountDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.PhoneNumber,
                user.User,
                user.Bio,
                user.Address
       
            });
        }

        [HttpPut("account")]
        public async Task<IActionResult> UpdateAccount(UpdateAccountModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.Name = model.Name ?? user.Name;
            user.PhoneNumber = model.Phone ?? user.PhoneNumber;
            user.Address = model.Address ?? user.Address;
            user.Bio = model.Bio ?? user.Bio;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to update account.", Errors = result.Errors });
            }

            return Ok(new { Message = "Account updated successfully.", ImagePath = user.ImagePath });
        }



    }

}

