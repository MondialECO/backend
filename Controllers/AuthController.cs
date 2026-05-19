using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
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
using WebApp.Services;
//using static Org.BouncyCastle.Math.EC.ECCurve;
namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly SaveFile _fileService;
        private readonly IDistributedCache _cache;
        private readonly TwilioService _twilioService;
        private readonly WebApp.Services.Audit.IAuditLogger _audit;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            RoleManager<ApplicationRole> roleManager,
            EmailService emailSender,
            ILogger<AuthController> logger,
            SaveFile saveFile,
            IDistributedCache cache,
            TwilioService twilioService,
            WebApp.Services.Audit.IAuditLogger audit
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _emailService = emailSender;
            _logger = logger;
            _fileService = saveFile;
            _cache = cache;
            _twilioService = twilioService;
            _audit = audit;
        }

        #region Helper Methods

        private IActionResult Success(string message, object? data = null)
        {
            return Ok(new
            {
                success = true,
                message,
                data
            });
        }

        private IActionResult Fail(string message)
        {
            return BadRequest(new
            {
                success = false,
                message
            });
        }

        private IActionResult NotFoundResponse(string message)
        {
            return NotFound(new
            {
                success = false,
                message
            });
        }

        private IActionResult UnauthorizedResponse(string message)
        {
            return Unauthorized(new
            {
                success = false,
                message
            });
        }

        private IActionResult InternalError(string message)
        {
            return StatusCode(500, new
            {
                success = false,
                message
            });
        }

        #endregion



        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Fail("Invalid request data");


                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _audit.Record("login", model.Email, false, new { reason = "unknown_email" });
                    return UnauthorizedResponse("Invalid email or password");
                }

                if (!user.EmailConfirmed)
                {
                    return UnauthorizedResponse("You need to confirm your email before logging in.");
                }

                if (!await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    _audit.Record("login", model.Email, false, new { reason = "bad_password" });
                    return UnauthorizedResponse("Invalid email or password");
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                    return UnauthorizedResponse("Your account is locked. Please try again later.");

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
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning("Failed to update user with refresh token");
                    // still return tokens to user, but log warning
                }

                _audit.Record("login", user.Email ?? model.Email, true, new { role });

                return Success("Logged in successfully", new
                {
                    token,
                    user = new
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Roles = roles
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return InternalError("An unexpected error occurred. Please try again later.");
            }
        }



        //public class TokenRefreshRequest
        //{
        //    public string AccessToken { get; set; } = "";
        //    public string RefreshToken { get; set; } = "";
        //}

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFoundResponse("User not found.");

            return Success("User retrieved", new
            {
                user.Id,
                user.Name,
                user.Email,
                user.PhoneNumber,
                user.User,
                user.Bio
            });
        }


        //[HttpPost("refresh")]
        //public async Task<IActionResult> Refresh(TokenRefreshRequest request)
        //{
        //    try
        //    {
        //        var principal = JwtTokenHelper.GetPrincipalFromExpiredToken(
        //            request.AccessToken,
        //           _configuration["JwtSettings:Key"],
        //            _configuration["JwtSettings:Issuer"],
        //            _configuration["JwtSettings:Audience"]
        //        );

        //        var userId = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        //        if (userId == null)
        //            return UnauthorizedResponse("Invalid access token");

        //        var user = await _userManager.FindByIdAsync(userId);
        //        if (user == null)
        //            return UnauthorizedResponse("User not found");

        //        if (user.RefreshToken == null ||
        //            user.RefreshToken.Token != request.RefreshToken ||
        //            user.RefreshToken.ExpiresAt < DateTime.UtcNow ||
        //            user.RefreshToken.IsRevoked)
        //        {
        //            return UnauthorizedResponse("Invalid refresh token");
        //        }

        //        var newAccessToken = JwtTokenHelper.GenerateToken(
        //            user.Id.ToString(),
        //            (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User",
        //            _configuration["JwtSettings:Key"],
        //            _configuration["JwtSettings:Issuer"],
        //            _configuration["JwtSettings:Audience"]
        //        );

        //        var newRefreshToken = JwtTokenHelper.GenerateRefreshToken();

        //        user.RefreshToken.Token = newRefreshToken;
        //        user.RefreshToken.ExpiresAt = DateTime.UtcNow.AddDays(7);

        //        await _userManager.UpdateAsync(user);

        //        return Success("Token refreshed", new
        //        {
        //            accessToken = newAccessToken,
        //            refreshToken = newRefreshToken
        //        });
        //    }
        //    catch (SecurityTokenException ste)
        //    {
        //        _logger.LogWarning(ste, "Invalid token during refresh");
        //        return UnauthorizedResponse("Invalid token");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error refreshing token");
        //        return InternalError("Failed to refresh token");
        //    }
        //}

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Success("Logged out");

            await _userManager.UpdateAsync(user);
            await _signInManager.SignOutAsync();
            return Success("Logged out");
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return Fail("Invalid request data");

            if (string.Equals(model.User, "Admin", StringComparison.OrdinalIgnoreCase))
                return Fail("Invalid role");

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                _audit.Record("register", model.Email, false, new { reason = "email_in_use" });
                return Fail("Email already in use");
            }

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
                return Fail("User registration failed");

            var roleExists = await _roleManager.RoleExistsAsync(model.User);
            if (!roleExists)
            {
                return InternalError("Failed to create default role");
            }

            await _userManager.AddToRoleAsync(user, model.User);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedToken = WebUtility.UrlEncode(token);

            var confirmationLink = $"https://mondialbusiness.eu/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&token={encodedToken}";

            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirm Email</a>");

            if (!emailSent)
                return InternalError("User registered, but failed to send confirmation email.");

            _audit.Record("register", user.Email!, true, new { role = model.User });

            return StatusCode(201, new { success = true, message = "User registered successfully! Please check your email for confirmation.", data = new { user.Id, user.Email } });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
        {
            if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.Token))
            {
                return Fail("Invalid confirmation request. User ID and Token are required.");
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFoundResponse("User not found.");
            }

            if (user.EmailConfirmed)
            {
                return Success("Your email is already confirmed. You can log in now.");
            }
            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (!result.Succeeded)
            {
                return Fail("Email confirmation failed. Invalid or expired token.");
            }

            return Success("Email confirmed successfully! You can now log in.");
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
                return Fail("Invalid email");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _audit.Record("forgot_password", model.Email, false, new { reason = "unknown_email" });
                return NotFoundResponse("User not found");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var resetUrl = $"https://mondialbusiness.eu/reset-password?email={model.Email}&token={encodedToken}";

            _logger.LogInformation($"Generated password reset token for {model.Email}");
            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Password Reset",
                $"Click here to reset your password: <a href='{resetUrl}'>Reset Password</a>");

            if (!emailSent)
                return InternalError("Failed to send reset password email.");

            _audit.Record("forgot_password", user.Email!, true);
            return Success("Password reset link sent to your email.");
        }


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestModel model)
        {
            if (!ModelState.IsValid)
                return Fail("Invalid request data");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFoundResponse("User not found");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!result.Succeeded)
            {
                _audit.Record("reset_password", user.Email!, false, new { reason = "bad_or_expired_token" });
                return Fail("Invalid or expired token.");
            }

            _logger.LogInformation($"Password successfully reset for {model.Email}");
            _audit.Record("reset_password", user.Email!, true);
            return Success("Password reset successfully.");
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
                    return UnauthorizedResponse("User not authenticated.");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFoundResponse("User not found.");

                var result = await _userManager.ChangePasswordAsync(user, model.currentPassword, model.newPassword);
                if (!result.Succeeded)
                {
                    _audit.Record("change_password", user.Email ?? userId, false, new { reason = "bad_current_password" });
                    return Fail("Password change failed.");
                }

                _logger.LogInformation($"Password successfully changed for user {user.Email}");
                _audit.Record("change_password", user.Email ?? userId, true);

                return Success("Password changed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return InternalError("Internal server error");
            }
        }

        [HttpGet("account")]
        public async Task<IActionResult> GetAccountDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFoundResponse("User not found.");

            return Success("Account details retrieved", new
            {
                user.Id,
                user.Name,
                user.Email,
                user.PhoneNumber,
                user.User,
                user.Bio,
                user.Address,
                user.Roles,
                user.AvailableTime,
                user.Geography,
                user.Experience,
                user.MainExperience,

            });
        }

        [HttpPut("account")]
        public async Task<IActionResult> UpdateAccount(UpdateAccountModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFoundResponse("User not found.");

            user.Name = model.Name ?? user.Name;
            user.PhoneNumber = model.Phone ?? user.PhoneNumber;
            user.Address = model.Address ?? user.Address;
            user.Bio = model.Bio ?? user.Bio;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Fail("Failed to update account.");
            }

            return Success("Account updated successfully.", new { ImagePath = user.ImagePath });
        }


      
    }


}

