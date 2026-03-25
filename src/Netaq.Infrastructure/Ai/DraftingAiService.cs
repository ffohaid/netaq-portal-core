using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Netaq.Application.Ai.Commands;
using Netaq.Application.Tenders.Queries;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Encryption;

namespace Netaq.Infrastructure.Ai;

/// <summary>
/// AI Drafting Service that dynamically selects the AI provider based on
/// database-driven AiConfiguration. Supports Gemini (Cloud) and Ollama (On-Premise).
/// API keys are decrypted only in-memory during execution.
/// </summary>
public class DraftingAiService : IAiDraftingService
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DraftingAiService> _logger;

    public DraftingAiService(
        IApplicationDbContext context,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory,
        ILogger<DraftingAiService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
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
        var config = await GetActiveAiConfigAsync(organizationId, cancellationToken);
        if (config == null)
            return new AiCriteriaSuggestionDto(new List<TenderCriteriaDto>(), "No active AI configuration found.", "None", "None");

        var prompt = BuildCriteriaSuggestionPrompt(titleAr, titleEn, descriptionAr, descriptionEn, tenderType, criteriaType, additionalContext);
        var response = await SendAiRequestAsync(config, prompt, cancellationToken);

        try
        {
            var criteria = ParseCriteriaSuggestions(response, criteriaType);
            return new AiCriteriaSuggestionDto(
                criteria,
                "AI-suggested criteria based on tender type and description.",
                config.ProviderType.ToString(),
                config.ModelName
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI criteria suggestions. Returning raw response.");
            return new AiCriteriaSuggestionDto(
                new List<TenderCriteriaDto>(),
                response,
                config.ProviderType.ToString(),
                config.ModelName
            );
        }
    }

    public async Task<AiComplianceCheckDto> CheckLegalComplianceAsync(
        string titleAr, string titleEn,
        TenderType tenderType,
        List<SectionContent> sections,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var config = await GetActiveAiConfigAsync(organizationId, cancellationToken);
        if (config == null)
            return new AiComplianceCheckDto(false, new List<ComplianceIssue>(), "No active AI configuration found.", "None", "None");

        var prompt = BuildComplianceCheckPrompt(titleAr, titleEn, tenderType, sections);
        var response = await SendAiRequestAsync(config, prompt, cancellationToken);

        try
        {
            _logger.LogInformation("Raw AI response length: {Length}, first 200 chars: {Preview}", 
                response.Length, response.Length > 200 ? response.Substring(0, 200) : response);
            var result = ParseComplianceCheck(response);
            _logger.LogInformation("Parsed compliance check: IsCompliant={IsCompliant}, Issues={IssueCount}, Summary length={SummaryLen}",
                result.IsCompliant, result.Issues.Count, result.Summary.Length);
            return new AiComplianceCheckDto(
                result.IsCompliant,
                result.Issues,
                result.Summary,
                config.ProviderType.ToString(),
                config.ModelName
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI compliance check. Raw response first 500 chars: {Preview}", 
                response.Length > 500 ? response.Substring(0, 500) : response);
            // Try to extract a meaningful summary from the raw response
            var extractedJson = ExtractJsonFromResponse(response);
            _logger.LogWarning("Extracted JSON first 500 chars: {Preview}", 
                extractedJson.Length > 500 ? extractedJson.Substring(0, 500) : extractedJson);
            return new AiComplianceCheckDto(
                false,
                new List<ComplianceIssue>(),
                response,
                config.ProviderType.ToString(),
                config.ModelName
            );
        }
    }

    public async Task<AiSuggestionDto> GenerateBoilerplateAsync(
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        TenderType tenderType, BookletSectionType sectionType,
        string? additionalContext,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var config = await GetActiveAiConfigAsync(organizationId, cancellationToken);
        if (config == null)
            return new AiSuggestionDto("No active AI configuration found.", "None", "None", 0);

        var prompt = BuildBoilerplatePrompt(titleAr, titleEn, descriptionAr, descriptionEn, tenderType, sectionType, additionalContext);
        var response = await SendAiRequestAsync(config, prompt, cancellationToken);

        return new AiSuggestionDto(
            response,
            config.ProviderType.ToString(),
            config.ModelName,
            0.85
        );
    }

    // ==================== Private Helpers ====================

    private async Task<Domain.Entities.AiConfiguration?> GetActiveAiConfigAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return await _context.AiConfigurations
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string> SendAiRequestAsync(Domain.Entities.AiConfiguration config, string prompt, CancellationToken cancellationToken)
    {
        var apiKey = !string.IsNullOrEmpty(config.ApiKeyEncrypted)
            ? _encryptionService.Decrypt(config.ApiKeyEncrypted)
            : null;

        return config.ProviderType switch
        {
            AiProviderType.Gemini => await SendGeminiRequestAsync(config, apiKey!, prompt, cancellationToken),
            AiProviderType.OpenAI => await SendOpenAiCompatibleRequestAsync(config, apiKey!, prompt, cancellationToken),
            AiProviderType.Ollama or AiProviderType.Aya => await SendOllamaRequestAsync(config, prompt, cancellationToken),
            _ => throw new NotSupportedException($"AI provider type {config.ProviderType} is not supported.")
        };
    }

    private async Task<string> SendGeminiRequestAsync(Domain.Entities.AiConfiguration config, string apiKey, string prompt, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        // Remove trailing slash from endpoint if present
        var baseEndpoint = config.Endpoint.TrimEnd('/');
        // Check if endpoint already contains /v1beta
        var endpoint = baseEndpoint.EndsWith("/v1beta") || baseEndpoint.EndsWith("/v1")
            ? $"{baseEndpoint}/models/{config.ModelName}:generateContent?key={apiKey}"
            : $"{baseEndpoint}/v1beta/models/{config.ModelName}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = config.Temperature,
                maxOutputTokens = config.MaxTokens
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
        }

        // Parse Gemini response
        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }

    private async Task<string> SendOpenAiCompatibleRequestAsync(Domain.Entities.AiConfiguration config, string apiKey, string prompt, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var endpoint = $"{config.Endpoint}/v1/chat/completions";

        var requestBody = new
        {
            model = config.ModelName,
            messages = new[]
            {
                new { role = "system", content = "You are an expert Saudi government procurement advisor. Respond in both Arabic and English." },
                new { role = "user", content = prompt }
            },
            temperature = config.Temperature,
            max_tokens = config.MaxTokens
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"OpenAI API returned {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? string.Empty;
    }

    private async Task<string> SendOllamaRequestAsync(Domain.Entities.AiConfiguration config, string prompt, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var endpoint = $"{config.Endpoint}/api/generate";

        var requestBody = new
        {
            model = config.ModelName,
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = config.Temperature,
                num_predict = config.MaxTokens
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Ollama API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Ollama API returned {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
    }

    // ==================== Prompt Builders ====================

    private static string BuildCriteriaSuggestionPrompt(
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        TenderType tenderType, CriteriaType criteriaType,
        string? additionalContext)
    {
        var contextLine = additionalContext != null ? $"- Additional Context: {additionalContext}" : "";
        return $@"You are a Saudi government procurement advisor. Suggest {criteriaType} evaluation criteria.

Tender: {titleAr} / {titleEn}
Type: {tenderType}
{contextLine}

IMPORTANT INSTRUCTIONS:
- Respond with ONLY valid JSON. No markdown, no code blocks, no extra text.
- Provide exactly 4 main criteria with weights summing to 100%.
- Each criterion has max 2 children.
- Keep descriptions SHORT (max 80 chars each).
- Rationale max 200 chars.

JSON format:
{{
  ""criteria"": [{{
    ""nameAr"": ""..."", ""nameEn"": ""..."",
    ""descriptionAr"": ""..."", ""descriptionEn"": ""..."",
    ""weight"": 30, ""passingThreshold"": 60,
    ""children"": [{{
      ""nameAr"": ""..."", ""nameEn"": ""..."",
      ""descriptionAr"": ""..."", ""descriptionEn"": ""..."",
      ""weight"": 50, ""passingThreshold"": null
    }}]
  }}],
  ""rationale"": ""Brief explanation.""
}}";
    }

    private static string BuildComplianceCheckPrompt(
        string titleAr, string titleEn,
        TenderType tenderType,
        List<SectionContent> sections)
    {
        var sectionsText = new StringBuilder();
        foreach (var section in sections)
        {
            sectionsText.AppendLine($"### {section.Title}");
            sectionsText.AppendLine(section.Content);
            sectionsText.AppendLine();
        }

        return $@"You are a legal compliance expert for Saudi government procurement.
        
Review the following tender booklet for compliance with:
1. Saudi Government Tenders and Procurement Law (نظام المنافسات والمشتريات الحكومية)
2. Its implementing regulations
3. NCA cybersecurity controls (where applicable)
4. Local content requirements

Tender: {titleAr} / {titleEn}
Type: {tenderType}

Booklet Content:
{sectionsText}

IMPORTANT INSTRUCTIONS:
- Respond with ONLY valid JSON, no markdown, no code blocks, no extra text.
- Keep issue descriptions concise (max 100 characters each).
- Keep suggestions concise (max 150 characters each).
- Summary should be max 300 characters.
- List only the top 5 most critical issues.

JSON format:
{{
  ""isCompliant"": true,
  ""issues"": [{{
    ""sectionTitle"": ""Section name"",
    ""issue"": ""Brief issue description"",
    ""suggestion"": ""Brief fix suggestion"",
    ""severity"": ""High""
  }}],
  ""summary"": ""Brief overall assessment.""
}}";
    }

    private static string BuildBoilerplatePrompt(
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        TenderType tenderType, BookletSectionType sectionType,
        string? additionalContext)
    {
        var sectionName = sectionType switch
        {
            BookletSectionType.GeneralTermsAndConditions => "General Terms and Conditions / الشروط والأحكام العامة",
            BookletSectionType.TechnicalScopeAndSpecifications => "Technical Scope and Specifications / النطاق الفني والمواصفات",
            BookletSectionType.QualificationRequirements => "Qualification Requirements / متطلبات التأهيل",
            BookletSectionType.EvaluationCriteria => "Evaluation Criteria / معايير التقييم",
            BookletSectionType.FinancialTerms => "Financial Terms / الشروط المالية",
            BookletSectionType.ContractualTerms => "Contractual Terms / الشروط التعاقدية",
            BookletSectionType.LocalContentRequirements => "Local Content Requirements / متطلبات المحتوى المحلي",
            BookletSectionType.AppendicesAndForms => "Appendices and Forms / الملاحق والنماذج",
            _ => "Unknown Section"
        };

        var contextLine2 = additionalContext != null ? $"- Additional Context: {additionalContext}" : "";
        return $@"You are an expert Saudi government procurement document writer.
        
        Generate professional boilerplate content for the following tender booklet section:
        
        Tender Information:
        - Title (AR): {titleAr}
        - Title (EN): {titleEn}
        - Description (AR): {descriptionAr}
        - Description (EN): {descriptionEn}
        - Tender Type: {tenderType}
        - Section: {sectionName}
        {contextLine2}
        
        Requirements:
        1. Content must comply with Saudi Government Tenders and Procurement Law.
        2. Write in professional Arabic (primary) with English technical terms where appropriate.
        3. Use proper HTML formatting (headings, lists, tables where needed).
        4. Include standard clauses required by Saudi procurement regulations.
        5. Make the content specific to the tender type ({tenderType}).
        6. Include placeholders like [اسم الجهة] for organization-specific data.
        
        Generate the HTML content for this section:";
    }

    // ==================== Response Parsers ====================

    private static List<TenderCriteriaDto> ParseCriteriaSuggestions(string response, CriteriaType criteriaType)
    {
        // Try to extract JSON from the response
        var jsonStr = ExtractJsonFromResponse(response);
        using var doc = JsonDocument.Parse(jsonStr);

        var criteriaArray = doc.RootElement.GetProperty("criteria");
        var result = new List<TenderCriteriaDto>();
        int order = 1;

        foreach (var item in criteriaArray.EnumerateArray())
        {
            var children = new List<TenderCriteriaDto>();
            int childOrder = 1;

            if (item.TryGetProperty("children", out var childrenProp))
            {
                foreach (var child in childrenProp.EnumerateArray())
                {
                    children.Add(new TenderCriteriaDto(
                        Guid.NewGuid(), null,
                        child.GetProperty("nameAr").GetString() ?? "",
                        child.GetProperty("nameEn").GetString() ?? "",
                        child.TryGetProperty("descriptionAr", out var dAr) ? dAr.GetString() : null,
                        child.TryGetProperty("descriptionEn", out var dEn) ? dEn.GetString() : null,
                        criteriaType,
                        child.GetProperty("weight").GetDecimal(),
                        child.TryGetProperty("passingThreshold", out var pt) && pt.ValueKind != JsonValueKind.Null ? pt.GetDecimal() : null,
                        childOrder++,
                        true,
                        new List<TenderCriteriaDto>()
                    ));
                }
            }

            result.Add(new TenderCriteriaDto(
                Guid.NewGuid(), null,
                item.GetProperty("nameAr").GetString() ?? "",
                item.GetProperty("nameEn").GetString() ?? "",
                item.TryGetProperty("descriptionAr", out var descAr) ? descAr.GetString() : null,
                item.TryGetProperty("descriptionEn", out var descEn) ? descEn.GetString() : null,
                criteriaType,
                item.GetProperty("weight").GetDecimal(),
                item.TryGetProperty("passingThreshold", out var threshold) && threshold.ValueKind != JsonValueKind.Null ? threshold.GetDecimal() : null,
                order++,
                true,
                children
            ));
        }

        return result;
    }

    private static string GetJsonStringOrSerialize(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? "";
        return element.GetRawText();
    }

    private static string ExtractJsonFromResponse(string response)
    {
        // Remove markdown code blocks if present
        var cleaned = response.Trim();
        if (cleaned.Contains("```json"))
        {
            var start = cleaned.IndexOf("```json") + 7;
            var end = cleaned.IndexOf("```", start);
            if (end > start)
                cleaned = cleaned.Substring(start, end - start).Trim();
        }
        else if (cleaned.Contains("```"))
        {
            var start = cleaned.IndexOf("```") + 3;
            // Skip to next line
            var newline = cleaned.IndexOf('\n', start);
            if (newline > 0) start = newline + 1;
            var end = cleaned.IndexOf("```", start);
            if (end > start)
                cleaned = cleaned.Substring(start, end - start).Trim();
        }

        // Find balanced JSON object
        var jsonStart = cleaned.IndexOf('{');
        if (jsonStart < 0) return cleaned;

        int depth = 0;
        int jsonEnd = -1;
        bool inString = false;
        bool escaped = false;

        for (int i = jsonStart; i < cleaned.Length; i++)
        {
            char c = cleaned[i];
            if (escaped) { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) { jsonEnd = i; break; }
            }
        }

        if (jsonEnd > jsonStart)
            return cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);

        // JSON is truncated - try to repair by closing open brackets/braces
        var truncated = cleaned.Substring(jsonStart);
        // Count open brackets and braces
        int openBraces = 0;
        int openBrackets = 0;
        bool inStr = false;
        bool esc = false;
        int lastValidPos = truncated.Length - 1;
        
        for (int i = 0; i < truncated.Length; i++)
        {
            char ch = truncated[i];
            if (esc) { esc = false; continue; }
            if (ch == '\\' && inStr) { esc = true; continue; }
            if (ch == '"') { inStr = !inStr; continue; }
            if (inStr) continue;
            if (ch == '{') openBraces++;
            else if (ch == '}') openBraces--;
            else if (ch == '[') openBrackets++;
            else if (ch == ']') openBrackets--;
        }
        
        // If we're inside a string, close it
        if (inStr) truncated += "\"";
        
        // Remove any trailing incomplete key-value pair
        // Find the last complete value (ends with , or } or ] or " or digit)
        var repaired = truncated.TrimEnd();
        // Remove trailing comma if any
        if (repaired.EndsWith(",")) repaired = repaired.Substring(0, repaired.Length - 1);
        
        // Close open brackets and braces
        for (int i = 0; i < openBrackets; i++) repaired += "]";
        for (int i = 0; i < openBraces; i++) repaired += "}";
        
        return repaired;
    }

    private static AiComplianceCheckDto ParseComplianceCheck(string response)
    {
        var jsonStr = ExtractJsonFromResponse(response);
        using var doc = JsonDocument.Parse(jsonStr);

        var isCompliant = doc.RootElement.TryGetProperty("isCompliant", out var compProp) && compProp.ValueKind == JsonValueKind.True;
        var summary = doc.RootElement.TryGetProperty("summary", out var summaryProp)
            ? GetJsonStringOrSerialize(summaryProp)
            : "";

        var issues = new List<ComplianceIssue>();
        if (doc.RootElement.TryGetProperty("issues", out var issuesArray) && issuesArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var issue in issuesArray.EnumerateArray())
            {
                var sectionTitle = issue.TryGetProperty("sectionTitle", out var st) ? GetJsonStringOrSerialize(st) : "";
                var issueTxt = issue.TryGetProperty("issue", out var iss) ? GetJsonStringOrSerialize(iss) : "";
                var suggestion = issue.TryGetProperty("suggestion", out var sug) ? GetJsonStringOrSerialize(sug) : "";
                var severity = issue.TryGetProperty("severity", out var sev) ? GetJsonStringOrSerialize(sev) : "Medium";
                issues.Add(new ComplianceIssue(sectionTitle, issueTxt, suggestion, severity));
            }
        }

        return new AiComplianceCheckDto(isCompliant, issues, summary, "", "");
    }
}
