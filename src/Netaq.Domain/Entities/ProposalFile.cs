using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a file attached to a vendor proposal.
/// Files are stored in MinIO with AES-256 encryption.
/// Supports PDF files up to 100MB.
/// </summary>
public class ProposalFile : BaseEntity
{
    /// <summary>
    /// Reference to the parent proposal.
    /// </summary>
    public Guid ProposalId { get; set; }

    /// <summary>
    /// Original file name as uploaded.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored file name in MinIO (UUID-based for uniqueness).
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// MinIO bucket name where the file is stored.
    /// </summary>
    public string BucketName { get; set; } = "proposals";

    /// <summary>
    /// Full object key/path in MinIO storage.
    /// </summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type of the file.
    /// </summary>
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// SHA-256 hash of the file content for integrity verification.
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Category of the file (technical offer, financial offer, etc.).
    /// </summary>
    public ProposalFileCategory Category { get; set; } = ProposalFileCategory.TechnicalOffer;

    /// <summary>
    /// Whether AI has extracted text from this file.
    /// </summary>
    public bool IsTextExtracted { get; set; } = false;

    /// <summary>
    /// AI-extracted text content from the file (for search and analysis).
    /// </summary>
    public string? ExtractedText { get; set; }

    // Navigation properties
    public Proposal Proposal { get; set; } = null!;
}
