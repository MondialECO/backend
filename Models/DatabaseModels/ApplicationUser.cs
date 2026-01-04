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

        [BsonElement("KycStatus")]
        public string KycStatus { get; set; } // Pending, Approved, Rejected

        [BsonElement("WalletBalance")]
        public double WalletBalance { get; set; }

        [BsonElement("LastLogin")]
        public DateTime LastLogin { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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


}
