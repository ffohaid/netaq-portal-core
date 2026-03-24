using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Infrastructure.Ai;

/// <summary>
/// Background service that automatically indexes approved tender booklets
/// into the Qdrant vector database for RAG (Knowledge Base).
/// Runs periodically to check for newly approved tenders that haven't been indexed yet.
/// </summary>
public class DocumentIndexingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentIndexingBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public DocumentIndexingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DocumentIndexingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Indexing Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await IndexNewlyApprovedDocumentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in document indexing background service.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task IndexNewlyApprovedDocumentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var ragService = scope.ServiceProvider.GetRequiredService<IRagService>();

        // Find approved tenders that have sections with content but haven't been indexed
        // We use a simple approach: check for approved tenders where sections have content
        var approvedTenders = await context.Tenders
            .AsNoTracking()
            .Where(t => t.Status == TenderStatus.Approved || t.Status == TenderStatus.Published)
            .Include(t => t.Sections)
            .ToListAsync(cancellationToken);

        foreach (var tender in approvedTenders)
        {
            var sectionsWithContent = tender.Sections
                .Where(s => !string.IsNullOrWhiteSpace(s.ContentHtml))
                .ToList();

            if (sectionsWithContent.Count == 0) continue;

            // Build full document content from all sections
            var fullContent = string.Join("\n\n",
                sectionsWithContent.Select(s =>
                    $"## {s.TitleAr} / {s.TitleEn}\n{StripHtml(s.ContentHtml ?? "")}"
                ));

            var metadata = new Dictionary<string, string>
            {
                ["tender_type"] = tender.TenderType.ToString(),
                ["tender_status"] = tender.Status.ToString(),
                ["reference_number"] = tender.ReferenceNumber,
                ["document_type"] = "approved_booklet",
            };

            var success = await ragService.IndexDocumentAsync(
                tender.OrganizationId,
                tender.Id.ToString(),
                $"{tender.TitleAr} / {tender.TitleEn}",
                fullContent,
                metadata,
                cancellationToken
            );

            if (success)
            {
                _logger.LogInformation(
                    "Successfully indexed tender {TenderId} ({Reference}) into knowledge base",
                    tender.Id, tender.ReferenceNumber
                );
            }
        }
    }

    /// <summary>
    /// Simple HTML tag stripper for indexing purposes.
    /// </summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // Remove HTML tags
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ");
        // Normalize whitespace
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
        return result.Trim();
    }
}
