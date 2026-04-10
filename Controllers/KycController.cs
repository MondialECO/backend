using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class KycController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly SaveFile _fileService;
        private readonly IDistributedCache _cache;
        private readonly TwilioService _twilioService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MongoDbContext _context;

        public KycController(EmailService emailService,
            ILogger<AuthController> logger, SaveFile fileService,
            IDistributedCache cache, TwilioService twilioService,
            UserManager<ApplicationUser> userManager,
             MongoDbContext context)
        {
            _emailService = emailService;
            _logger = logger;
            _fileService = fileService;
            _cache = cache;
            _twilioService = twilioService;
            _userManager = userManager;
            _context = context;
        }


        public class IdentityUploadDto
        {
            public string DocumentType { get; set; } // NID / Passport

            public IFormFile FrontImage { get; set; }
            public IFormFile BackImage { get; set; }
        }

        [HttpPost("identity/upload")]
        public async Task<IActionResult> UploadIdentity([FromForm] IdentityUploadDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            var frontPath = await _fileService.SaveFileAsync(dto.FrontImage, "Identity");
            var backPath = await _fileService.SaveFileAsync(dto.BackImage, "Identity");

            user.Kyc.Identity = new IdentityVerification
            {
                DocumentType = dto.DocumentType,
                //DocumentNumber = dto.DocumentNumber,
                FrontImage = frontPath,
                BackImage = backPath,
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            await _userManager.UpdateAsync(user);

            return Ok("Identity submitted for review");
        }


        public class FaceUploadDto
        {
            public IFormFile Image { get; set; }
        }

        [HttpPost("face/upload")]
        public async Task<IActionResult> UploadFace([FromForm] FaceUploadDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            var selfiePath = await _fileService.SaveFileAsync(dto.Image, "Face");

            user.Kyc.Face = new FacialVerification
            {
                SelfieImage = selfiePath,
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            await _userManager.UpdateAsync(user);

            return Ok("Face submitted for review");
        }


        [HttpPost("send-email-otp")]
        public async Task<IActionResult> SendEmailOtp()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.EmailConfirmed)
                return BadRequest("Email already verified");

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Store in Redis (5 min expiry)
            await _cache.SetStringAsync($"email_otp:{user.Id}", otp,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            // Send Email
            await _emailService.SendEmailAsync(user.Email, "Your OTP Code",
                $"Your verification OTP is: {otp}");

            return Ok("OTP sent to email");
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
                return BadRequest("OTP expired");

            if (storedOtp != dto.Otp)
                return BadRequest("Invalid OTP");

            // Mark email as verified
            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);

            // Remove OTP after success
            await _cache.RemoveAsync($"email_otp:{user.Id}");

            return Ok("Email verified successfully");
        }

        [HttpPost("resend-email-otp")]
        public async Task<IActionResult> ResendOtp()
        {
            var user = await _userManager.GetUserAsync(User);

            var cooldownKey = $"otp_cooldown:{user.Id}";

            var exists = await _cache.GetStringAsync(cooldownKey);

            if (exists != null)
                return BadRequest("Wait before requesting again");

            await _cache.SetStringAsync(cooldownKey, "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                });

            return await SendEmailOtp();
        }



        public class PhoneNumber
        {
            public string Number { get; set; }
        }


        [HttpPost("send-phone-otp")]
        public async Task<IActionResult> SendPhoneOtp([FromBody] PhoneNumber dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.PhoneNumberConfirmed)
                return BadRequest("Phone already verified");

            user.PhoneNumber = dto.Number;
            await _userManager.UpdateAsync(user);

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Store in Redis (5 min expiry)
            await _cache.SetStringAsync($"phone_otp:{user.Id}", otp,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            //  Send SMS via Twilio
            await _twilioService.SendSmsAsync(user.PhoneNumber, $"Your OTP is: {otp}");

            return Ok("OTP sent to phone");
        }


        public class VerifyPhoneOtpDto
        {
            public string Otp { get; set; }
        }

        [HttpPost("verify-phone-otp")]
        public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            var storedOtp = await _cache.GetStringAsync($"phone_otp:{user.Id}");

            if (storedOtp == null)
                return BadRequest("OTP expired");

            if (storedOtp != dto.Otp)
                return BadRequest("Invalid OTP");

            // Mark phone as verified
            user.PhoneNumberConfirmed = true;

            await _userManager.UpdateAsync(user);

            // Remove OTP after success
            await _cache.RemoveAsync($"phone_otp:{user.Id}");

            return Ok("Phone verified successfully");
        }

        [HttpPost("resend-phone-otp")]
        public async Task<IActionResult> ResendPhoneOtp()
        {
            var user = await _userManager.GetUserAsync(User);

            var cooldownKey = $"phone_otp_cooldown:{user.Id}";

            var exists = await _cache.GetStringAsync(cooldownKey);

            if (exists != null)
                return BadRequest("Wait before requesting again");

            await _cache.SetStringAsync(cooldownKey, "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                });

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Store in Redis (5 min expiry)
            await _cache.SetStringAsync($"phone_otp:{user.Id}", otp,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            //  Send SMS via Twilio
            await _twilioService.SendSmsAsync(user.PhoneNumber, $"Your OTP is: {otp}");

            return Ok("OTP sent to phone");
        }







        public class RejectKycDto
        {
            public string Reason { get; set; }
        }


        // varification pending list
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingUsers()
        {
            var users = await _context.ApplicationUsers
                .Find(x => x.Kyc.Status == VerificationStatus.Pending)
                .ToListAsync();

            return Ok(users);
        }

        // verificaton approve 
        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{userId}")]
        public async Task<IActionResult> ApproveKyc(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            user.Kyc.Identity.Status = VerificationStatus.Verified;
            user.Kyc.Face.Status = VerificationStatus.Verified;
            user.Kyc.Status = VerificationStatus.Verified;
            user.Kyc.VerifiedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return Ok("KYC Approved");
        }


        // verification rejected
        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{userId}")]
        public async Task<IActionResult> RejectKyc(Guid userId, [FromBody] RejectKycDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            user.Kyc.Status = VerificationStatus.Rejected;
            user.Kyc.Identity.Status = VerificationStatus.Rejected;
            user.Kyc.Identity.RejectionReason = dto.Reason;

            await _userManager.UpdateAsync(user);

            return Ok("KYC Rejected");
        }
    }
}
