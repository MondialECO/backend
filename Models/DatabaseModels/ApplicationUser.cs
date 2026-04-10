using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace WebApp.Models.DatabaseModels
{
    public class ApplicationUser : MongoIdentityUser<Guid>
    {
        [BsonElement("Name")]
        public string Name { get; set; }
        public string User { get; set; }

        [BsonElement("Phone")]
        public string Phone { get; set; }

        [BsonElement("Address")]
        public Address Address { get; set; }

        [BsonElement("ImagePath")]
        public string ImagePath { get; set; }

        [BsonElement("RefreshToken")]
        public RefreshToken RefreshToken { get; set; } = new();

        //[BsonElement("RefreshTokenExpiryTime")]
        //public DateTime RefreshTokenExpiryTime { get; set; }

        [BsonElement("Bio")]
        public string Bio { get; set; }
        public string AvailableTime { get; set; }
        public string Geography { get; set; }
        public string Experience { get; set; }
        public string MainExperience { get; set; }
        public string linkedin_url { get; set; }

        [BsonElement("KycStatus")]
        public string KycStatus { get; set; } // Pending, Approved, Rejected

        [BsonElement("WalletBalance")]
        public double WalletBalance { get; set; }
        public int Tier_level { get; set; }
        public int Trust_score { get; set; }
        public int investor_ready_score { get; set; }


        [BsonElement("LastLogin")]
        public DateTime LastLogin { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public KycVerification Kyc { get; set; } = new();
    }

    public class Address
    {
        public string address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class RefreshToken
    {
        public ObjectId Id { get; set; }

        //public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByIp { get; set; } = "";
    }

    public class KycVerification
    {
        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public IdentityVerification Identity { get; set; } = new();
        public FacialVerification Face { get; set; } = new();

        public DateTime? VerifiedAt { get; set; }
    }

    public class IdentityVerification
    {
        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public string DocumentType { get; set; } // NID / Passport / DL
        public string DocumentNumber { get; set; }

        public string FrontImage { get; set; }
        public string BackImage { get; set; }

        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public string RejectionReason { get; set; }
    }

    public class FacialVerification
    {
        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        //public string SelfieImage { get; set; }

        //public double? MatchScore { get; set; } // AI ready

        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public string RejectionReason { get; set; }
    }

    public enum VerificationStatus
    {
        Pending,
        Verified,
        Rejected
    }
}
