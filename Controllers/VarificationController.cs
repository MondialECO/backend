using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using WebApp.DbContext;
using WebApp.Models.DatabaseModels;
using WebApp.Services;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VarificationController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly SaveFile _fileService;
        private readonly IDistributedCache _cache;
        private readonly TwilioService _twilioService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MongoDbContext _context;

        public VarificationController(
            EmailService emailService,
            SaveFile fileService,
            IDistributedCache cache,
            TwilioService twilioService,
            UserManager<ApplicationUser> userManager,
            MongoDbContext context)
        {
            _emailService = emailService;
            _fileService = fileService;
            _cache = cache;
            _twilioService = twilioService;
            _userManager = userManager;
            _context = context;
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

        #endregion

        public class IdentityUploadDto
        {
            public string DocumentType { get; set; }
            public IFormFile FrontImage { get; set; }
            public IFormFile BackImage { get; set; }
        }

        [HttpPost("identity")]
        public async Task<IActionResult> UploadIdentity([FromForm] IdentityUploadDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            var frontPath = await _fileService.SaveFileAsync(dto.FrontImage, "Identity");
            var backPath = await _fileService.SaveFileAsync(dto.BackImage, "Identity");

            user.Kyc.Identity = new IdentityVerification
            {
                DocumentType = dto.DocumentType,
                FrontImage = frontPath,
                BackImage = backPath,
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            await _userManager.UpdateAsync(user);

            return Success("Identity submitted for review");
        }

        [HttpPost("face")]
        public async Task<IActionResult> UploadFace()
        {
            var user = await _userManager.GetUserAsync(User);

            user.Kyc.Face = new FacialVerification
            {
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                VerifiedAt = DateTime.UtcNow
            };

            await _userManager.UpdateAsync(user);

            return Success("Face submitted for review");
        }

        [HttpPost("send-email-otp")]
        public async Task<IActionResult> SendEmailOtp()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.EmailConfirmed)
                return Fail("Email already verified");

            var otp = new Random().Next(100000, 999999).ToString();

            await _cache.SetStringAsync($"email_otp:{user.Id}", otp,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            await _emailService.SendEmailAsync(
                user.Email,
                "Your OTP Code",
                $"Your verification OTP is: {otp}");

            return Success("OTP sent to email");
        }

        public class VerifyOtpDto
        {
            public string Otp { get; set; }
        }

        [HttpPost("verify-email-otp")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyOtpDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            var storedOtp = await _cache.GetStringAsync($"email_otp:{user.Id}");

            if (storedOtp == null)
                return Fail("OTP expired");

            if (storedOtp != dto.Otp)
                return Fail("Invalid OTP");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _cache.RemoveAsync($"email_otp:{user.Id}");

            return Success("Email verified successfully");
        }

        [HttpPost("resend-email-otp")]
        public async Task<IActionResult> ResendOtp()
        {
            var user = await _userManager.GetUserAsync(User);

            var cooldownKey = $"otp_cooldown:{user.Id}";
            var exists = await _cache.GetStringAsync(cooldownKey);

            if (exists != null)
                return Fail("Wait before requesting again");

            await _cache.SetStringAsync(cooldownKey, "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                });

            return await SendEmailOtp();
        }

        public class PhoneNumberDto
        {
            public string Number { get; set; }
        }

        [HttpPost("send-phone-otp")]
        public async Task<IActionResult> SendPhoneOtp([FromBody] PhoneNumberDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.PhoneNumberConfirmed)
                return Fail("Phone already verified");

            user.PhoneNumber = dto.Number;
            await _userManager.UpdateAsync(user);

            var otp = new Random().Next(100000, 999999).ToString();

            await _cache.SetStringAsync($"phone_otp:{user.Id}", otp,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            await _twilioService.SendSmsAsync(user.PhoneNumber, $"Your OTP is: {otp}");

            return Success("OTP sent to phone");
        }

        [HttpPost("verify-phone-otp")]
        public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyOtpDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            var storedOtp = await _cache.GetStringAsync($"phone_otp:{user.Id}");

            if (storedOtp == null)
                return Fail("OTP expired");

            if (storedOtp != dto.Otp)
                return Fail("Invalid OTP");

            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _cache.RemoveAsync($"phone_otp:{user.Id}");

            return Success("Phone verified successfully");
        }

        [HttpPost("resend-phone-otp")]
        public async Task<IActionResult> ResendPhoneOtp()
        {
            var user = await _userManager.GetUserAsync(User);

            var cooldownKey = $"phone_otp_cooldown:{user.Id}";
            var exists = await _cache.GetStringAsync(cooldownKey);

            if (exists != null)
                return Fail("Wait before requesting again");

            await _cache.SetStringAsync(cooldownKey, "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                });

            return await SendPhoneOtp(new PhoneNumberDto
            {
                Number = user.PhoneNumber
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingUsers()
        {
            var users = await _context.ApplicationUsers
                .Find(x => x.Kyc.Status == VerificationStatus.Pending)
                .ToListAsync();

            return Success("Pending users fetched successfully", users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{userId}")]
        public async Task<IActionResult> ApproveKyc(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return NotFoundResponse("User not found");

            user.Kyc.Identity.Status = VerificationStatus.Verified;
            user.Kyc.Face.Status = VerificationStatus.Verified;
            user.Kyc.Status = VerificationStatus.Verified;
            user.Kyc.VerifiedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return Success("KYC Approved");
        }

        public class RejectKycDto
        {
            public string Reason { get; set; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{userId}")]
        public async Task<IActionResult> RejectKyc(Guid userId, [FromBody] RejectKycDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return NotFoundResponse("User not found");

            user.Kyc.Status = VerificationStatus.Rejected;
            user.Kyc.Identity.Status = VerificationStatus.Rejected;
            user.Kyc.Identity.RejectionReason = dto.Reason;

            await _userManager.UpdateAsync(user);

            return Success("KYC Rejected");
        }
    }
}