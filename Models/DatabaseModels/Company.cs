using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels;

public class Company
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = default!;

    public string LegalName { get; set; } = string.Empty;

    public string? SiretNumber { get; set; }

    public string? VatNumber { get; set; }

    public string? LegalStructure { get; set; }

    public string? RegisteredAddress { get; set; }

    public string? NafCode { get; set; }

    [BsonRepresentation(BsonType.String)]
    public VerifiStatus VerificationStatus { get; set; } = VerifiStatus.Pending;

    public bool VerifiedBadge { get; set; } = false;

    public bool IsSubmittedForReview { get; set; } = false;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? ReviewedByAdminId { get; set; }

    public string? VerificationNotes { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CompanyDocument> Documents { get; set; } = new();
}

public class CompanyDocument
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.String)]
    public CompanyDocumentType DocType { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public int? FileSizeKb { get; set; }

    [BsonRepresentation(BsonType.String)]
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionNote { get; set; }
}

public enum VerifiStatus
{
    Pending,
    InReview,
    Verified,
    Rejected
}

public enum CompanyDocumentType
{
    Kbis,
    Rib,
    TaxCert,
    Insurance,
    Other
}

public enum DocumentStatus
{
    Uploaded,
    Reviewing,
    Approved,
    Rejected
}