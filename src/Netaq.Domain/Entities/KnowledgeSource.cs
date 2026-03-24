using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a knowledge base source document for RAG indexing.
/// Tracks both auto-indexed approved booklets and manually uploaded permanent content.
/// </summary>
public class KnowledgeSource : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Source type: AutoIndexed (from approved booklets) or ManualUpload (permanent content).
    /// </summary>
    public KnowledgeSourceType SourceType { get; set; }
    
    /// <summary>
    /// Display title for the knowledge source.
    /// </summary>
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the content.
    /// </summary>
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    
    /// <summary>
    /// Reference to the source tender (for auto-indexed booklets).
    /// </summary>
    public Guid? TenderId { get; set; }
    
    /// <summary>
    /// File URL in MinIO storage (for manual uploads).
    /// </summary>
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// Raw text content extracted from the document.
    /// </summary>
    public string? ContentText { get; set; }
    
    /// <summary>
    /// Indexing status.
    /// </summary>
    public KnowledgeIndexingStatus IndexingStatus { get; set; } = KnowledgeIndexingStatus.Pending;
    
    /// <summary>
    /// Total number of text chunks generated from this source.
    /// </summary>
    public int TotalChunks { get; set; }
    
    /// <summary>
    /// Total number of vectors stored in Qdrant for this source.
    /// </summary>
    public int TotalVectors { get; set; }
    
    /// <summary>
    /// Last time this source was indexed.
    /// </summary>
    public DateTime? LastIndexedAt { get; set; }
    
    /// <summary>
    /// Error message if indexing failed.
    /// </summary>
    public string? IndexingError { get; set; }
    
    /// <summary>
    /// Document ID used in the vector database.
    /// </summary>
    public string? VectorDocumentId { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Tender? Tender { get; set; }
}
