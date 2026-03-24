using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Ai.Services;

/// <summary>
/// AI service for tender evaluation operations.
/// Provides 6 features:
/// 1. Proposal summarization
/// 2. Auto-mapping with booklet requirements
/// 3. Gap analysis
/// 4. Score suggestion with justification
/// 5. Comparison matrix between proposals
/// 6. Award justification draft
/// 
/// Uses AiConfiguration for API keys with AES-256 decryption in memory only.
/// </summary>
public interface IEvaluationAiService
{
    Task<AiSummarizationResult> SummarizeProposalAsync(Guid proposalId, CancellationToken cancellationToken = default);
    Task<AiAutoMappingResult> AutoMapRequirementsAsync(Guid proposalId, CancellationToken cancellationToken = default);
    Task<AiGapAnalysisResult> AnalyzeGapsAsync(Guid proposalId, CancellationToken cancellationToken = default);
    Task<AiScoreSuggestionResult> SuggestScoresAsync(Guid proposalId, CancellationToken cancellationToken = default);
    Task<AiComparisonMatrixResult> GenerateComparisonMatrixAsync(Guid tenderId, CancellationToken cancellationToken = default);
    Task<AiAwardJustificationResult> GenerateAwardJustificationAsync(Guid tenderId, CancellationToken cancellationToken = default);
}

// ===== Result DTOs =====
public class AiSummarizationResult
{
    public bool Success { get; set; }
    public string SummaryAr { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public List<string> KeyStrengths { get; set; } = new();
    public List<string> KeyWeaknesses { get; set; } = new();
    public string? Error { get; set; }
}

public class AiAutoMappingResult
{
    public bool Success { get; set; }
    public List<RequirementMapping> Mappings { get; set; } = new();
    public decimal CoveragePercentage { get; set; }
    public string? Error { get; set; }
}

public class RequirementMapping
{
    public string RequirementAr { get; set; } = string.Empty;
    public string RequirementEn { get; set; } = string.Empty;
    public string ProposalSection { get; set; } = string.Empty;
    public string CoverageLevel { get; set; } = string.Empty; // Full, Partial, Missing
    public string? Notes { get; set; }
}

public class AiGapAnalysisResult
{
    public bool Success { get; set; }
    public List<GapItem> Gaps { get; set; } = new();
    public string OverallAssessmentAr { get; set; } = string.Empty;
    public string OverallAssessmentEn { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class GapItem
{
    public string RequirementAr { get; set; } = string.Empty;
    public string GapDescriptionAr { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Critical, Major, Minor
    public string? RecommendationAr { get; set; }
}

public class AiScoreSuggestionResult
{
    public bool Success { get; set; }
    public List<CriteriaScoreSuggestion> Suggestions { get; set; } = new();
    public decimal SuggestedTotalScore { get; set; }
    public string? Error { get; set; }
}

public class CriteriaScoreSuggestion
{
    public Guid CriteriaId { get; set; }
    public string CriteriaNameAr { get; set; } = string.Empty;
    public decimal SuggestedScore { get; set; }
    public string JustificationAr { get; set; } = string.Empty;
    public string JustificationEn { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
}

public class AiComparisonMatrixResult
{
    public bool Success { get; set; }
    public List<ComparisonDimension> Dimensions { get; set; } = new();
    public string OverallAnalysisAr { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class ComparisonDimension
{
    public string DimensionAr { get; set; } = string.Empty;
    public string DimensionEn { get; set; } = string.Empty;
    public List<ProposalComparison> Proposals { get; set; } = new();
}

public class ProposalComparison
{
    public Guid ProposalId { get; set; }
    public string VendorNameAr { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty; // Excellent, Good, Average, Poor
    public string NotesAr { get; set; } = string.Empty;
}

public class AiAwardJustificationResult
{
    public bool Success { get; set; }
    public string JustificationAr { get; set; } = string.Empty;
    public string JustificationEn { get; set; } = string.Empty;
    public string RecommendedVendorAr { get; set; } = string.Empty;
    public List<string> KeyReasons { get; set; } = new();
    public string? Error { get; set; }
}

/// <summary>
/// Implementation of EvaluationAiService using OpenAI-compatible API.
/// API keys are retrieved from AiConfiguration with AES-256 decryption.
/// </summary>
public class EvaluationAiService : IEvaluationAiService
{
    private readonly IApplicationDbContext _context;
    private readonly IAiConfigurationService _aiConfig;

    public EvaluationAiService(IApplicationDbContext context, IAiConfigurationService aiConfig)
    {
        _context = context;
        _aiConfig = aiConfig;
    }

    public async Task<AiSummarizationResult> SummarizeProposalAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await _context.Proposals
                .Include(p => p.Files)
                .Include(p => p.Tender)
                .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

            if (proposal == null)
                return new AiSummarizationResult { Success = false, Error = "Proposal not found." };

            // Get extracted text from files
            var extractedTexts = proposal.Files
                .Where(f => f.IsTextExtracted && !string.IsNullOrEmpty(f.ExtractedText))
                .Select(f => f.ExtractedText!)
                .ToList();

            var combinedText = string.Join("\n\n", extractedTexts);
            if (string.IsNullOrWhiteSpace(combinedText))
                combinedText = $"Vendor: {proposal.VendorNameAr}, Total Value: {proposal.TotalValue:N2} SAR, Reference: {proposal.VendorReferenceNumber}";

            var prompt = $@"أنت خبير في تحليل العروض الحكومية السعودية. قم بتلخيص العرض التالي المقدم لمنافسة ""{proposal.Tender.TitleAr}"":

{combinedText}

قدم:
1. ملخص شامل باللغة العربية (200-300 كلمة)
2. ملخص باللغة الإنجليزية
3. أبرز نقاط القوة (3-5 نقاط)
4. أبرز نقاط الضعف (3-5 نقاط)

أجب بصيغة JSON:
{{""summaryAr"": ""..."", ""summaryEn"": ""..."", ""strengths"": [""...""], ""weaknesses"": [""...""]}}";

            var response = await _aiConfig.SendAiRequestAsync(prompt, cancellationToken);
            
            if (response != null)
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(response);
                return new AiSummarizationResult
                {
                    Success = true,
                    SummaryAr = parsed.GetProperty("summaryAr").GetString() ?? "",
                    SummaryEn = parsed.GetProperty("summaryEn").GetString() ?? "",
                    KeyStrengths = parsed.GetProperty("strengths").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
                    KeyWeaknesses = parsed.GetProperty("weaknesses").EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                };
            }

            // Fallback: generate basic summary
            return GenerateFallbackSummary(proposal);
        }
        catch (Exception ex)
        {
            return new AiSummarizationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<AiAutoMappingResult> AutoMapRequirementsAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await _context.Proposals
                .Include(p => p.Files)
                .Include(p => p.Tender).ThenInclude(t => t.Sections)
                .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

            if (proposal == null)
                return new AiAutoMappingResult { Success = false, Error = "Proposal not found." };

            var requirements = proposal.Tender.Sections
                .Where(s => !string.IsNullOrEmpty(s.ContentHtml))
                .Select(s => new { s.TitleAr, s.ContentHtml })
                .ToList();

            var extractedTexts = proposal.Files
                .Where(f => f.IsTextExtracted && !string.IsNullOrEmpty(f.ExtractedText))
                .Select(f => f.ExtractedText!)
                .ToList();

            var prompt = $@"أنت خبير في مطابقة العروض مع متطلبات كراسة الشروط. قم بمطابقة محتوى العرض مع متطلبات الكراسة:

متطلبات الكراسة:
{JsonSerializer.Serialize(requirements)}

محتوى العرض:
{string.Join("\n", extractedTexts)}

لكل متطلب، حدد مستوى التغطية (Full/Partial/Missing).
أجب بصيغة JSON:
{{""mappings"": [{{""requirementAr"": ""..."", ""requirementEn"": ""..."", ""proposalSection"": ""..."", ""coverageLevel"": ""Full|Partial|Missing"", ""notes"": ""...""}}], ""coveragePercentage"": 85}}";

            var response = await _aiConfig.SendAiRequestAsync(prompt, cancellationToken);

            if (response != null)
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(response);
                var mappings = parsed.GetProperty("mappings").EnumerateArray().Select(m => new RequirementMapping
                {
                    RequirementAr = m.GetProperty("requirementAr").GetString() ?? "",
                    RequirementEn = m.GetProperty("requirementEn").GetString() ?? "",
                    ProposalSection = m.GetProperty("proposalSection").GetString() ?? "",
                    CoverageLevel = m.GetProperty("coverageLevel").GetString() ?? "Missing",
                    Notes = m.TryGetProperty("notes", out var n) ? n.GetString() : null
                }).ToList();

                return new AiAutoMappingResult
                {
                    Success = true,
                    Mappings = mappings,
                    CoveragePercentage = parsed.GetProperty("coveragePercentage").GetDecimal()
                };
            }

            return new AiAutoMappingResult { Success = true, Mappings = new(), CoveragePercentage = 0 };
        }
        catch (Exception ex)
        {
            return new AiAutoMappingResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<AiGapAnalysisResult> AnalyzeGapsAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await _context.Proposals
                .Include(p => p.Files)
                .Include(p => p.Tender).ThenInclude(t => t.Sections)
                .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

            if (proposal == null)
                return new AiGapAnalysisResult { Success = false, Error = "Proposal not found." };

            var prompt = $@"أنت خبير في تحليل فجوات العروض الحكومية. قم بتحليل الفجوات بين العرض ومتطلبات كراسة الشروط لمنافسة ""{proposal.Tender.TitleAr}"".

أجب بصيغة JSON:
{{""gaps"": [{{""requirementAr"": ""..."", ""gapDescriptionAr"": ""..."", ""severity"": ""Critical|Major|Minor"", ""recommendationAr"": ""...""}}], ""overallAssessmentAr"": ""..."", ""overallAssessmentEn"": ""...""}}";

            var response = await _aiConfig.SendAiRequestAsync(prompt, cancellationToken);

            if (response != null)
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(response);
                return new AiGapAnalysisResult
                {
                    Success = true,
                    Gaps = parsed.GetProperty("gaps").EnumerateArray().Select(g => new GapItem
                    {
                        RequirementAr = g.GetProperty("requirementAr").GetString() ?? "",
                        GapDescriptionAr = g.GetProperty("gapDescriptionAr").GetString() ?? "",
                        Severity = g.GetProperty("severity").GetString() ?? "Minor",
                        RecommendationAr = g.TryGetProperty("recommendationAr", out var r) ? r.GetString() : null
                    }).ToList(),
                    OverallAssessmentAr = parsed.GetProperty("overallAssessmentAr").GetString() ?? "",
                    OverallAssessmentEn = parsed.GetProperty("overallAssessmentEn").GetString() ?? ""
                };
            }

            return new AiGapAnalysisResult { Success = true, Gaps = new(), OverallAssessmentAr = "لا تتوفر بيانات كافية للتحليل" };
        }
        catch (Exception ex)
        {
            return new AiGapAnalysisResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<AiScoreSuggestionResult> SuggestScoresAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await _context.Proposals
                .Include(p => p.Files)
                .Include(p => p.Tender).ThenInclude(t => t.Criteria)
                .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

            if (proposal == null)
                return new AiScoreSuggestionResult { Success = false, Error = "Proposal not found." };

            var technicalCriteria = proposal.Tender.Criteria
                .Where(c => c.CriteriaType == CriteriaType.Technical)
                .ToList();

            var leafCriteria = technicalCriteria
                .Where(c => !technicalCriteria.Any(ch => ch.ParentId == c.Id))
                .OrderBy(c => c.OrderIndex)
                .ToList();

            var criteriaList = leafCriteria.Select(c => new
            {
                c.Id,
                c.NameAr,
                c.NameEn,
                c.Weight,
                c.PassingThreshold,
                c.DescriptionAr
            });

            var prompt = $@"أنت خبير في تقييم العروض الحكومية السعودية. بناءً على محتوى العرض ومعايير التقييم، اقترح درجات لكل معيار:

المعايير:
{JsonSerializer.Serialize(criteriaList)}

قدم لكل معيار: درجة مقترحة (0-100)، مبرر بالعربية والإنجليزية، ومستوى الثقة.
أجب بصيغة JSON:
{{""suggestions"": [{{""criteriaId"": ""..."", ""criteriaNameAr"": ""..."", ""suggestedScore"": 85, ""justificationAr"": ""..."", ""justificationEn"": ""..."", ""confidence"": 0.8}}], ""suggestedTotalScore"": 82}}";

            var response = await _aiConfig.SendAiRequestAsync(prompt, cancellationToken);

            if (response != null)
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(response);
                var suggestions = parsed.GetProperty("suggestions").EnumerateArray().Select(s => new CriteriaScoreSuggestion
                {
                    CriteriaId = Guid.Parse(s.GetProperty("criteriaId").GetString() ?? Guid.Empty.ToString()),
                    CriteriaNameAr = s.GetProperty("criteriaNameAr").GetString() ?? "",
                    SuggestedScore = s.GetProperty("suggestedScore").GetDecimal(),
                    JustificationAr = s.GetProperty("justificationAr").GetString() ?? "",
                    JustificationEn = s.GetProperty("justificationEn").GetString() ?? "",
                    Confidence = s.GetProperty("confidence").GetDecimal()
                }).ToList();

                // Store AI suggestions in EvaluationScore records
                foreach (var suggestion in suggestions)
                {
                    var existingScores = await _context.EvaluationScores
                        .Where(es => es.ProposalId == proposalId && es.CriteriaId == suggestion.CriteriaId)
                        .ToListAsync(cancellationToken);

                    foreach (var score in existingScores)
                    {
                        score.AiSuggestedScore = suggestion.SuggestedScore;
                        score.AiJustification = suggestion.JustificationAr;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                return new AiScoreSuggestionResult
                {
                    Success = true,
                    Suggestions = suggestions,
                    SuggestedTotalScore = parsed.GetProperty("suggestedTotalScore").GetDecimal()
                };
            }

            return new AiScoreSuggestionResult { Success = true, Suggestions = new(), SuggestedTotalScore = 0 };
        }
        catch (Exception ex)
        {
            return new AiScoreSuggestionResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<AiComparisonMatrixResult> GenerateComparisonMatrixAsync(Guid tenderId, CancellationToken cancellationToken)
    {
        try
        {
            var proposals = await _context.Proposals
                .Where(p => p.TenderId == tenderId && p.PassedComplianceCheck == true)
                .ToListAsync(cancellationToken);

            if (!proposals.Any())
                return new AiComparisonMatrixResult { Success = false, Error = "No eligible proposals found." };

            var prompt = $@"أنت خبير في مقارنة العروض الحكومية. قم بإنشاء مصفوفة مقارنة بين العروض التالية:

{JsonSerializer.Serialize(proposals.Select(p => new {{ p.VendorNameAr, p.TotalValue, p.TechnicalScore }}))}

قدم مقارنة على أبعاد: الجودة الفنية، السعر، الخبرة، الجدول الزمني، فريق العمل.
أجب بصيغة JSON:
{{""dimensions"": [{{""dimensionAr"": ""..."", ""dimensionEn"": ""..."", ""proposals"": [{{""proposalId"": ""..."", ""vendorNameAr"": ""..."", ""rating"": ""Excellent|Good|Average|Poor"", ""notesAr"": ""...""}}]}}], ""overallAnalysisAr"": ""...""}}";

            var response = await _aiConfig.SendAiRequestAsync(prompt, cancellationToken);

            if (response != null)
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(response);
                return new AiComparisonMatrixResult
                {
                    Success = true,
                    Dimensions = parsed.GetProperty("dimensions").EnumerateArray().Select(d => new ComparisonDimension
                    {
                        DimensionAr = d.GetProperty("dimensionAr").GetString() ?? "",
                        DimensionEn = d.GetProperty("dimensionEn").GetString() ?? "",
                        Proposals = d.GetProperty("proposals").EnumerateArray().Select(p => new ProposalComparison
                        {
                            ProposalId = Guid.TryParse(p.GetProperty("proposalId").GetString(), out var id) ? id : Guid.Empty,
                            VendorNameAr = p.GetProperty("vendorNameAr").GetString() ?? "",
                            Rating = p.GetProperty("rating").GetString() ?? "Average",
                            NotesAr = p.GetProperty("notesAr").GetString() ?? ""
                        }).ToList()
                    }).ToList(),
                    OverallAnalysisAr = parsed.GetProperty("overallAnalysisAr").GetString() ?? ""
                };
            }

            return new AiComparisonMatrixResult { Success = true, Dimensions = new() };
        }
        catch (Exception ex)
        {
            return new AiComparisonMatrixResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<AiAwardJustificationResult> GenerateAwardJustificationAsync(Guid tenderId, CancellationToken cancellationToken)
    {
        try
        {
            var tender = await _context.Tenders
                .Include(t => t.Proposals.Where(p => p.FinalRank != null))
                .FirstOrDefaultAsync(t => t.Id == tenderId, cancellationToken);

            if (tender == null)
                return new AiAwardJustificationResult { Success = false, Error = "Tender not found." };

            var rankedProposals = tender.Proposals.OrderBy(p => p.FinalRank).ToList();
            if (!rankedProposals.Any())
                return new AiAwardJustificationResult { Success = false, Error = "No ranked proposals found." };

            var topProposal = rankedProposals.First();

            var prompt = $@"أنت خبير قانوني في المشتريات الحكومية السعودية. قم بإعداد مسودة تقرير مبررات الترسية لمنافسة ""{tender.TitleAr}"":

المتنافس الموصى به: {topProposal.VendorNameAr}
الدرجة النهائية: {topProposal.FinalScore:F2}
الدرجة الفنية: {topProposal.TechnicalScore:F2}
الدرجة المالية: {topProposal.FinancialScore:F2}
القيمة المالية: {topProposal.TotalValue:N2} ريال

باقي المتنافسين:
{JsonSerializer.Serialize(rankedProposals.Skip(1).Select(p => new {{ p.VendorNameAr, p.FinalScore, p.FinalRank }}))}

قدم مبررات الترسية وفقاً لنظام المنافسات والمشتريات الحكومية.
أجب بصيغة JSON:
{{""justificationAr"": ""..."", ""justificationEn"": ""..."", ""recommendedVendorAr"": ""..."", ""keyReasons"": [""...""]}}";

            var response = await _aiConfig.SendAiRequestAsync(prompt, cancellationToken);

            if (response != null)
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(response);
                return new AiAwardJustificationResult
                {
                    Success = true,
                    JustificationAr = parsed.GetProperty("justificationAr").GetString() ?? "",
                    JustificationEn = parsed.GetProperty("justificationEn").GetString() ?? "",
                    RecommendedVendorAr = parsed.GetProperty("recommendedVendorAr").GetString() ?? "",
                    KeyReasons = parsed.GetProperty("keyReasons").EnumerateArray().Select(r => r.GetString() ?? "").ToList()
                };
            }

            return new AiAwardJustificationResult
            {
                Success = true,
                JustificationAr = $"يوصى بترسية المنافسة على {topProposal.VendorNameAr} بناءً على حصوله على أعلى درجة نهائية ({topProposal.FinalScore:F2})",
                RecommendedVendorAr = topProposal.VendorNameAr,
                KeyReasons = new() { "أعلى درجة نهائية", "استيفاء جميع المتطلبات الفنية" }
            };
        }
        catch (Exception ex)
        {
            return new AiAwardJustificationResult { Success = false, Error = ex.Message };
        }
    }

    private static AiSummarizationResult GenerateFallbackSummary(Proposal proposal)
    {
        return new AiSummarizationResult
        {
            Success = true,
            SummaryAr = $"عرض مقدم من {proposal.VendorNameAr} بقيمة إجمالية {proposal.TotalValue:N2} ريال سعودي. الرقم المرجعي: {proposal.VendorReferenceNumber}.",
            SummaryEn = $"Proposal submitted by {proposal.VendorNameEn} with total value of {proposal.TotalValue:N2} SAR. Reference: {proposal.VendorReferenceNumber}.",
            KeyStrengths = new() { "تم استلام العرض بنجاح" },
            KeyWeaknesses = new() { "لم يتم استخراج النص من الملفات بعد" }
        };
    }
}

/// <summary>
/// Interface for AI configuration service that handles API key management.
/// </summary>
public interface IAiConfigurationService
{
    Task<string?> SendAiRequestAsync(string prompt, CancellationToken cancellationToken = default);
}
