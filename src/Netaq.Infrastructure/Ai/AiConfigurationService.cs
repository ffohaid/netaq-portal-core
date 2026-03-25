using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Netaq.Application.Ai.Services;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Encryption;

namespace Netaq.Infrastructure.Ai;

/// <summary>
/// Implementation of IAiConfigurationService that dynamically selects the active AI provider
/// from the database and sends requests. Supports Gemini, OpenAI-Compatible, and Ollama.
/// API keys are decrypted only in-memory during execution.
/// </summary>
public class AiConfigurationService : IAiConfigurationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiConfigurationService> _logger;

    public AiConfigurationService(
        IApplicationDbContext context,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory,
        ILogger<AiConfigurationService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> SendAiRequestAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Get the active AI configuration
        var config = await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.IsActive, cancellationToken);

        if (config == null)
        {
            _logger.LogWarning("No active AI configuration found.");
            return null;
        }

        try
        {
            return config.ProviderType switch
            {
                AiProviderType.Gemini => await SendGeminiRequestAsync(config, prompt, cancellationToken),
                AiProviderType.OpenAI => await SendOpenAiCompatibleRequestAsync(config, prompt, cancellationToken),
                AiProviderType.Ollama or AiProviderType.Aya => await SendOllamaRequestAsync(config, prompt, cancellationToken),
                _ => throw new NotSupportedException($"AI provider type {config.ProviderType} is not supported.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending AI request to {Provider}", config.ProviderType);
            return null;
        }
    }

    private async Task<string> SendGeminiRequestAsync(
        Domain.Entities.AiConfiguration config, string prompt, CancellationToken cancellationToken)
    {
        var apiKey = _encryptionService.Decrypt(config.ApiKeyEncrypted!);
        var client = _httpClientFactory.CreateClient();

        var endpoint = $"{config.Endpoint}/v1beta/models/{config.ModelName}:generateContent?key={apiKey}";
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

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }

    private async Task<string> SendOpenAiCompatibleRequestAsync(
        Domain.Entities.AiConfiguration config, string prompt, CancellationToken cancellationToken)
    {
        var apiKey = _encryptionService.Decrypt(config.ApiKeyEncrypted!);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var endpoint = config.Endpoint!.TrimEnd('/');
        if (!endpoint.EndsWith("/chat/completions"))
            endpoint += "/v1/chat/completions";

        var requestBody = new
        {
            model = config.ModelName,
            messages = new[]
            {
                new { role = "system", content = "You are an expert Saudi government procurement advisor. Respond in both Arabic and English. Follow Saudi Government Tenders and Procurement Law." },
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
            _logger.LogError("OpenAI Compatible API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"OpenAI Compatible API returned {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? string.Empty;
    }

    private async Task<string> SendOllamaRequestAsync(
        Domain.Entities.AiConfiguration config, string prompt, CancellationToken cancellationToken)
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
}
