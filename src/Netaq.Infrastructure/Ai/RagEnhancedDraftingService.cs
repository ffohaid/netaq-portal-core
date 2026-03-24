using System.Text;
using Microsoft.Extensions.Logging;
using Netaq.Application.Ai.Commands;
using Netaq.Domain.Enums;

namespace Netaq.Infrastructure.Ai;

/// <summary>
/// Enhanced AI Drafting Service that uses RAG (Retrieval Augmented Generation)
/// to provide context-aware AI suggestions based on the organization's knowledge base.
/// This wraps the base DraftingAiService and enriches prompts with relevant context
/// retrieved from Qdrant vector database.
/// </summary>
public class RagEnhancedDraftingService : IAiDraftingService
{
    private readonly DraftingAiService _baseDraftingService;
    private readonly IRagService _ragService;
    private readonly ILogger<RagEnhancedDraftingService> _logger;

    public RagEnhancedDraftingService(
        DraftingAiService baseDraftingService,
        IRagService ragService,
        ILogger<RagEnhancedDraftingService> logger)
    {
        _baseDraftingService = baseDraftingService;
        _ragService = ragService;
        _logger = logger;
    }

    public async Task<AiCriteriaSuggestionDto> SuggestCriteriaAsync(
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        TenderType tenderType, CriteriaType criteriaType,
        string? additionalContext,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        // Retrieve relevant context from knowledge base
        var ragContext = await GetRagContextAsync(
            organizationId,
            $"{titleAr} {titleEn} {tenderType} {criteriaType} evaluation criteria",
            cancellationToken
        );

        var enrichedContext = EnrichContext(additionalContext, ragContext);

        return await _baseDraftingService.SuggestCriteriaAsync(
            titleAr, titleEn, descriptionAr, descriptionEn,
            tenderType, criteriaType, enrichedContext,
            organizationId, cancellationToken
        );
    }

    public async Task<AiComplianceCheckDto> CheckLegalComplianceAsync(
        string titleAr, string titleEn,
        TenderType tenderType,
        List<SectionContent> sections,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        // Compliance check uses the base service directly
        // RAG context is less relevant for legal compliance checks
        return await _baseDraftingService.CheckLegalComplianceAsync(
            titleAr, titleEn, tenderType, sections,
            organizationId, cancellationToken
        );
    }

    public async Task<AiSuggestionDto> GenerateBoilerplateAsync(
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        TenderType tenderType, BookletSectionType sectionType,
        string? additionalContext,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        // Retrieve relevant context from knowledge base
        var ragContext = await GetRagContextAsync(
            organizationId,
            $"{titleAr} {titleEn} {tenderType} {sectionType}",
            cancellationToken
        );

        var enrichedContext = EnrichContext(additionalContext, ragContext);

        return await _baseDraftingService.GenerateBoilerplateAsync(
            titleAr, titleEn, descriptionAr, descriptionEn,
            tenderType, sectionType, enrichedContext,
            organizationId, cancellationToken
        );
    }

    // ==================== Private Helpers ====================

    private async Task<string?> GetRagContextAsync(
        Guid organizationId, string query, CancellationToken cancellationToken)
    {
        try
        {
            var results = await _ragService.SearchAsync(organizationId, query, topK: 3, cancellationToken);

            if (results.Count == 0)
            {
                _logger.LogDebug("No RAG results found for query in organization {OrgId}", organizationId);
                return null;
            }

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("=== Relevant Knowledge Base Context ===");

            foreach (var result in results)
            {
                if (result.Score < 0.5) continue; // Skip low-relevance results

                contextBuilder.AppendLine($"--- From: {result.DocumentTitle} (Relevance: {result.Score:F2}) ---");
                contextBuilder.AppendLine(result.Content);
                contextBuilder.AppendLine();
            }

            var context = contextBuilder.ToString();
            _logger.LogDebug("RAG context retrieved: {Length} chars from {Count} results",
                context.Length, results.Count);

            return context.Length > 0 ? context : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve RAG context. Proceeding without it.");
            return null;
        }
    }

    private static string? EnrichContext(string? originalContext, string? ragContext)
    {
        if (originalContext == null && ragContext == null) return null;
        if (ragContext == null) return originalContext;
        if (originalContext == null) return ragContext;

        return $"{originalContext}\n\n{ragContext}";
    }
}
