using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Infrastructure.Ai;

namespace Netaq.Api.Controllers;

/// <summary>
/// Knowledge Base management endpoints for RAG (Retrieval Augmented Generation).
/// Allows indexing, searching, and managing documents in the vector database.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(IRagService ragService, ILogger<KnowledgeBaseController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Index a document into the knowledge base.
    /// Called automatically when a tender booklet is approved.
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> IndexDocument(
        [FromBody] IndexDocumentRequest request,
        CancellationToken cancellationToken)
    {
        // Get organization ID from claims
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var organizationId))
            return Unauthorized(new { error = "Organization context required" });

        var metadata = new Dictionary<string, string>
        {
            ["document_type"] = request.DocumentType ?? "booklet",
            ["tender_type"] = request.TenderType ?? "general",
            ["indexed_by"] = User.FindFirst("UserId")?.Value ?? "system",
            ["indexed_at"] = DateTime.UtcNow.ToString("O"),
        };

        var result = await _ragService.IndexDocumentAsync(
            organizationId,
            request.DocumentId,
            request.DocumentTitle,
            request.Content,
            metadata,
            cancellationToken
        );

        if (result)
            return Ok(new { isSuccess = true, data = new { message = "Document indexed successfully" } });

        return BadRequest(new { isSuccess = false, error = "Failed to index document. Check AI configuration." });
    }

    /// <summary>
    /// Search the knowledge base for relevant content.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromBody] SearchKnowledgeBaseRequest request,
        CancellationToken cancellationToken)
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var organizationId))
            return Unauthorized(new { error = "Organization context required" });

        var results = await _ragService.SearchAsync(
            organizationId,
            request.Query,
            request.TopK ?? 5,
            cancellationToken
        );

        return Ok(new
        {
            isSuccess = true,
            data = results.Select(r => new
            {
                r.DocumentId,
                r.DocumentTitle,
                r.Content,
                r.Score,
                r.Metadata
            })
        });
    }

    /// <summary>
    /// Delete a document from the knowledge base.
    /// </summary>
    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteDocument(
        string documentId,
        CancellationToken cancellationToken)
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var organizationId))
            return Unauthorized(new { error = "Organization context required" });

        var result = await _ragService.DeleteDocumentAsync(organizationId, documentId, cancellationToken);

        if (result)
            return Ok(new { isSuccess = true, data = new { message = "Document removed from knowledge base" } });

        return BadRequest(new { isSuccess = false, error = "Failed to delete document" });
    }

    /// <summary>
    /// Initialize the knowledge base collection for the organization.
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize(CancellationToken cancellationToken)
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var organizationId))
            return Unauthorized(new { error = "Organization context required" });

        await _ragService.EnsureCollectionAsync(organizationId, cancellationToken);

        return Ok(new { isSuccess = true, data = new { message = "Knowledge base initialized" } });
    }
}

// Request DTOs
public record IndexDocumentRequest(
    string DocumentId,
    string DocumentTitle,
    string Content,
    string? DocumentType,
    string? TenderType
);

public record SearchKnowledgeBaseRequest(
    string Query,
    int? TopK
);
