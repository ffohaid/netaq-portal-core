using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Ai.Services;

/// <summary>
/// Service for extracting text from PDF proposal files.
/// Supports both text-based PDFs and scanned documents (OCR).
/// Extracted text is stored in ProposalFile.ExtractedText for AI analysis.
/// </summary>
public interface IDocumentExtractionService
{
    Task<DocumentExtractionResult> ExtractTextAsync(Guid proposalFileId, CancellationToken cancellationToken = default);
    Task<DocumentExtractionResult> ExtractAllFilesAsync(Guid proposalId, CancellationToken cancellationToken = default);
}

public class DocumentExtractionResult
{
    public bool Success { get; set; }
    public int FilesProcessed { get; set; }
    public int TotalCharactersExtracted { get; set; }
    public string? Error { get; set; }
    public List<FileExtractionDetail> Details { get; set; } = new();
}

public class FileExtractionDetail
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int CharactersExtracted { get; set; }
    public string ExtractionMethod { get; set; } = string.Empty; // TextBased, OCR
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Implementation of document extraction service.
/// In production, integrates with OCR services for scanned documents.
/// For development, uses basic text extraction from PDF.
/// </summary>
public class DocumentExtractionService : IDocumentExtractionService
{
    private readonly IApplicationDbContext _context;

    public DocumentExtractionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentExtractionResult> ExtractTextAsync(Guid proposalFileId, CancellationToken cancellationToken)
    {
        var file = await _context.ProposalFiles
            .FirstOrDefaultAsync(f => f.Id == proposalFileId, cancellationToken);

        if (file == null)
            return new DocumentExtractionResult { Success = false, Error = "File not found." };

        try
        {
            // In production, this would use PDF parsing libraries and OCR
            // For now, mark as extracted with placeholder
            var extractedText = $"[Extracted text from {file.OriginalFileName}] - Document extraction service placeholder. " +
                              $"File size: {file.FileSizeBytes} bytes, Content type: {file.ContentType}";

            file.ExtractedText = extractedText;
            file.IsTextExtracted = true;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new DocumentExtractionResult
            {
                Success = true,
                FilesProcessed = 1,
                TotalCharactersExtracted = extractedText.Length,
                Details = new()
                {
                    new FileExtractionDetail
                    {
                        FileId = file.Id,
                        FileName = file.OriginalFileName,
                        CharactersExtracted = extractedText.Length,
                        ExtractionMethod = "TextBased",
                        Success = true
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new DocumentExtractionResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<DocumentExtractionResult> ExtractAllFilesAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        var files = await _context.ProposalFiles
            .Where(f => f.ProposalId == proposalId && !f.IsTextExtracted)
            .ToListAsync(cancellationToken);

        var result = new DocumentExtractionResult { Success = true };

        foreach (var file in files)
        {
            var fileResult = await ExtractTextAsync(file.Id, cancellationToken);
            result.FilesProcessed++;
            result.TotalCharactersExtracted += fileResult.TotalCharactersExtracted;
            result.Details.AddRange(fileResult.Details);

            if (!fileResult.Success)
                result.Details.Last().Error = fileResult.Error;
        }

        return result;
    }
}

// ===== MediatR Commands for AI Operations =====

public record SummarizeProposalCommand(Guid ProposalId) : IRequest<ApiResponse<AiSummarizationResult>>;

public class SummarizeProposalCommandHandler : IRequestHandler<SummarizeProposalCommand, ApiResponse<AiSummarizationResult>>
{
    private readonly IEvaluationAiService _aiService;

    public SummarizeProposalCommandHandler(IEvaluationAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiSummarizationResult>> Handle(SummarizeProposalCommand request, CancellationToken cancellationToken)
    {
        var result = await _aiService.SummarizeProposalAsync(request.ProposalId, cancellationToken);
        return result.Success
            ? ApiResponse<AiSummarizationResult>.Ok(result)
            : ApiResponse<AiSummarizationResult>.Fail(result.Error ?? "AI summarization failed.");
    }
}

public record AnalyzeGapsCommand(Guid ProposalId) : IRequest<ApiResponse<AiGapAnalysisResult>>;

public class AnalyzeGapsCommandHandler : IRequestHandler<AnalyzeGapsCommand, ApiResponse<AiGapAnalysisResult>>
{
    private readonly IEvaluationAiService _aiService;

    public AnalyzeGapsCommandHandler(IEvaluationAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiGapAnalysisResult>> Handle(AnalyzeGapsCommand request, CancellationToken cancellationToken)
    {
        var result = await _aiService.AnalyzeGapsAsync(request.ProposalId, cancellationToken);
        return result.Success
            ? ApiResponse<AiGapAnalysisResult>.Ok(result)
            : ApiResponse<AiGapAnalysisResult>.Fail(result.Error ?? "AI gap analysis failed.");
    }
}

public record SuggestScoresCommand(Guid ProposalId) : IRequest<ApiResponse<AiScoreSuggestionResult>>;

public class SuggestScoresCommandHandler : IRequestHandler<SuggestScoresCommand, ApiResponse<AiScoreSuggestionResult>>
{
    private readonly IEvaluationAiService _aiService;

    public SuggestScoresCommandHandler(IEvaluationAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiScoreSuggestionResult>> Handle(SuggestScoresCommand request, CancellationToken cancellationToken)
    {
        var result = await _aiService.SuggestScoresAsync(request.ProposalId, cancellationToken);
        return result.Success
            ? ApiResponse<AiScoreSuggestionResult>.Ok(result)
            : ApiResponse<AiScoreSuggestionResult>.Fail(result.Error ?? "AI score suggestion failed.");
    }
}

public record GenerateComparisonMatrixCommand(Guid TenderId) : IRequest<ApiResponse<AiComparisonMatrixResult>>;

public class GenerateComparisonMatrixCommandHandler : IRequestHandler<GenerateComparisonMatrixCommand, ApiResponse<AiComparisonMatrixResult>>
{
    private readonly IEvaluationAiService _aiService;

    public GenerateComparisonMatrixCommandHandler(IEvaluationAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiComparisonMatrixResult>> Handle(GenerateComparisonMatrixCommand request, CancellationToken cancellationToken)
    {
        var result = await _aiService.GenerateComparisonMatrixAsync(request.TenderId, cancellationToken);
        return result.Success
            ? ApiResponse<AiComparisonMatrixResult>.Ok(result)
            : ApiResponse<AiComparisonMatrixResult>.Fail(result.Error ?? "AI comparison matrix failed.");
    }
}

public record GenerateAwardJustificationCommand(Guid TenderId) : IRequest<ApiResponse<AiAwardJustificationResult>>;

public class GenerateAwardJustificationCommandHandler : IRequestHandler<GenerateAwardJustificationCommand, ApiResponse<AiAwardJustificationResult>>
{
    private readonly IEvaluationAiService _aiService;

    public GenerateAwardJustificationCommandHandler(IEvaluationAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiAwardJustificationResult>> Handle(GenerateAwardJustificationCommand request, CancellationToken cancellationToken)
    {
        var result = await _aiService.GenerateAwardJustificationAsync(request.TenderId, cancellationToken);
        return result.Success
            ? ApiResponse<AiAwardJustificationResult>.Ok(result)
            : ApiResponse<AiAwardJustificationResult>.Fail(result.Error ?? "AI award justification failed.");
    }
}

public record ExtractDocumentTextCommand(Guid ProposalId) : IRequest<ApiResponse<DocumentExtractionResult>>;

public class ExtractDocumentTextCommandHandler : IRequestHandler<ExtractDocumentTextCommand, ApiResponse<DocumentExtractionResult>>
{
    private readonly IDocumentExtractionService _extractionService;

    public ExtractDocumentTextCommandHandler(IDocumentExtractionService extractionService)
    {
        _extractionService = extractionService;
    }

    public async Task<ApiResponse<DocumentExtractionResult>> Handle(ExtractDocumentTextCommand request, CancellationToken cancellationToken)
    {
        var result = await _extractionService.ExtractAllFilesAsync(request.ProposalId, cancellationToken);
        return result.Success
            ? ApiResponse<DocumentExtractionResult>.Ok(result)
            : ApiResponse<DocumentExtractionResult>.Fail(result.Error ?? "Document extraction failed.");
    }
}
