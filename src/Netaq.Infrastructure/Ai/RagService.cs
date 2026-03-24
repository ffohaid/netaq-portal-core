using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Encryption;

namespace Netaq.Infrastructure.Ai;

/// <summary>
/// RAG (Retrieval Augmented Generation) service that integrates with Qdrant vector database.
/// Indexes approved documents into the knowledge base and retrieves relevant context for AI queries.
/// Qdrant endpoint is stored dynamically in AiConfiguration table.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Index a document into the vector database for future retrieval.
    /// </summary>
    Task<bool> IndexDocumentAsync(Guid organizationId, string documentId, string documentTitle,
        string content, Dictionary<string, string>? metadata, CancellationToken cancellationToken);

    /// <summary>
    /// Search the knowledge base for relevant context based on a query.
    /// </summary>
    Task<List<RagSearchResult>> SearchAsync(Guid organizationId, string query, int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a document from the vector database.
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid organizationId, string documentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ensure the collection exists for the organization.
    /// </summary>
    Task EnsureCollectionAsync(Guid organizationId, CancellationToken cancellationToken);
}

public record RagSearchResult(
    string DocumentId,
    string DocumentTitle,
    string Content,
    double Score,
    Dictionary<string, string>? Metadata
);

public class RagService : IRagService
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IApplicationDbContext context,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory,
        ILogger<RagService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> IndexDocumentAsync(
        Guid organizationId, string documentId, string documentTitle,
        string content, Dictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            var (qdrantEndpoint, aiConfig) = await GetQdrantConfigAsync(organizationId, cancellationToken);
            if (qdrantEndpoint == null || aiConfig == null)
            {
                _logger.LogWarning("No Qdrant configuration found for organization {OrgId}", organizationId);
                return false;
            }

            var collectionName = GetCollectionName(organizationId);
            await EnsureCollectionExistsAsync(qdrantEndpoint, collectionName, cancellationToken);

            // Generate embedding using the configured AI provider
            var embedding = await GenerateEmbeddingAsync(aiConfig, content, cancellationToken);
            if (embedding == null || embedding.Length == 0)
            {
                _logger.LogWarning("Failed to generate embedding for document {DocId}", documentId);
                return false;
            }

            // Split content into chunks for better retrieval
            var chunks = ChunkContent(content, maxChunkSize: 1000, overlap: 200);

            var client = _httpClientFactory.CreateClient();
            var points = new List<object>();
            int pointId = Math.Abs(documentId.GetHashCode());

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkEmbedding = await GenerateEmbeddingAsync(aiConfig, chunks[i], cancellationToken);
                if (chunkEmbedding == null) continue;

                var payload = new Dictionary<string, object>
                {
                    ["document_id"] = documentId,
                    ["document_title"] = documentTitle,
                    ["chunk_index"] = i,
                    ["content"] = chunks[i],
                    ["organization_id"] = organizationId.ToString(),
                };

                if (metadata != null)
                {
                    foreach (var kv in metadata)
                        payload[kv.Key] = kv.Value;
                }

                points.Add(new
                {
                    id = pointId + i,
                    vector = chunkEmbedding,
                    payload
                });
            }

            if (points.Count == 0) return false;

            var upsertBody = JsonSerializer.Serialize(new { points });
            var response = await client.PutAsync(
                $"{qdrantEndpoint}/collections/{collectionName}/points",
                new StringContent(upsertBody, Encoding.UTF8, "application/json"),
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Qdrant upsert failed: {Status} - {Body}", response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("Successfully indexed document {DocId} with {ChunkCount} chunks", documentId, points.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {DocId}", documentId);
            return false;
        }
    }

    public async Task<List<RagSearchResult>> SearchAsync(
        Guid organizationId, string query, int topK = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (qdrantEndpoint, aiConfig) = await GetQdrantConfigAsync(organizationId, cancellationToken);
            if (qdrantEndpoint == null || aiConfig == null)
                return new List<RagSearchResult>();

            var collectionName = GetCollectionName(organizationId);
            var queryEmbedding = await GenerateEmbeddingAsync(aiConfig, query, cancellationToken);
            if (queryEmbedding == null)
                return new List<RagSearchResult>();

            var client = _httpClientFactory.CreateClient();
            var searchBody = JsonSerializer.Serialize(new
            {
                vector = queryEmbedding,
                limit = topK,
                with_payload = true,
                filter = new
                {
                    must = new[]
                    {
                        new
                        {
                            key = "organization_id",
                            match = new { value = organizationId.ToString() }
                        }
                    }
                }
            });

            var response = await client.PostAsync(
                $"{qdrantEndpoint}/collections/{collectionName}/points/search",
                new StringContent(searchBody, Encoding.UTF8, "application/json"),
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Qdrant search failed: {Status} - {Body}", response.StatusCode, body);
                return new List<RagSearchResult>();
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseBody);

            var results = new List<RagSearchResult>();
            if (doc.RootElement.TryGetProperty("result", out var resultArray))
            {
                foreach (var item in resultArray.EnumerateArray())
                {
                    var payload = item.GetProperty("payload");
                    var score = item.GetProperty("score").GetDouble();

                    var docId = payload.TryGetProperty("document_id", out var did) ? did.GetString() ?? "" : "";
                    var docTitle = payload.TryGetProperty("document_title", out var dt) ? dt.GetString() ?? "" : "";
                    var content = payload.TryGetProperty("content", out var ct) ? ct.GetString() ?? "" : "";

                    var meta = new Dictionary<string, string>();
                    foreach (var prop in payload.EnumerateObject())
                    {
                        if (prop.Name != "document_id" && prop.Name != "document_title" && prop.Name != "content")
                        {
                            meta[prop.Name] = prop.Value.ToString();
                        }
                    }

                    results.Add(new RagSearchResult(docId, docTitle, content, score, meta));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Qdrant for organization {OrgId}", organizationId);
            return new List<RagSearchResult>();
        }
    }

    public async Task<bool> DeleteDocumentAsync(
        Guid organizationId, string documentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var (qdrantEndpoint, _) = await GetQdrantConfigAsync(organizationId, cancellationToken);
            if (qdrantEndpoint == null) return false;

            var collectionName = GetCollectionName(organizationId);
            var client = _httpClientFactory.CreateClient();

            var deleteBody = JsonSerializer.Serialize(new
            {
                filter = new
                {
                    must = new[]
                    {
                        new
                        {
                            key = "document_id",
                            match = new { value = documentId }
                        }
                    }
                }
            });

            var response = await client.PostAsync(
                $"{qdrantEndpoint}/collections/{collectionName}/points/delete",
                new StringContent(deleteBody, Encoding.UTF8, "application/json"),
                cancellationToken
            );

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocId} from Qdrant", documentId);
            return false;
        }
    }

    public async Task EnsureCollectionAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var (qdrantEndpoint, _) = await GetQdrantConfigAsync(organizationId, cancellationToken);
        if (qdrantEndpoint == null) return;

        var collectionName = GetCollectionName(organizationId);
        await EnsureCollectionExistsAsync(qdrantEndpoint, collectionName, cancellationToken);
    }

    // ==================== Private Helpers ====================

    private async Task<(string? QdrantEndpoint, Domain.Entities.AiConfiguration? AiConfig)> GetQdrantConfigAsync(
        Guid organizationId, CancellationToken cancellationToken)
    {
        var config = await _context.AiConfigurations
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null) return (null, null);

        return (config.QdrantEndpoint, config);
    }

    private static string GetCollectionName(Guid organizationId)
    {
        return $"netaq_kb_{organizationId:N}";
    }

    private async Task EnsureCollectionExistsAsync(string qdrantEndpoint, string collectionName, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        // Check if collection exists
        var checkResponse = await client.GetAsync($"{qdrantEndpoint}/collections/{collectionName}", cancellationToken);
        if (checkResponse.IsSuccessStatusCode) return;

        // Create collection with 1536 dimensions (standard embedding size)
        var createBody = JsonSerializer.Serialize(new
        {
            vectors = new
            {
                size = 1536,
                distance = "Cosine"
            }
        });

        var response = await client.PutAsync(
            $"{qdrantEndpoint}/collections/{collectionName}",
            new StringContent(createBody, Encoding.UTF8, "application/json"),
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create Qdrant collection: {Status} - {Body}", response.StatusCode, body);
        }
    }

    private async Task<float[]?> GenerateEmbeddingAsync(
        Domain.Entities.AiConfiguration config, string text, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = !string.IsNullOrEmpty(config.ApiKeyEncrypted)
                ? _encryptionService.Decrypt(config.ApiKeyEncrypted)
                : null;

            var client = _httpClientFactory.CreateClient();

            // Use OpenAI-compatible embedding endpoint
            if (config.ProviderType == Domain.Enums.AiProviderType.OpenAI ||
                config.ProviderType == Domain.Enums.AiProviderType.Gemini)
            {
                if (apiKey != null)
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var endpoint = config.ProviderType == Domain.Enums.AiProviderType.Gemini
                    ? $"{config.Endpoint}/v1beta/models/text-embedding-004:embedContent?key={apiKey}"
                    : $"{config.Endpoint}/v1/embeddings";

                if (config.ProviderType == Domain.Enums.AiProviderType.Gemini)
                {
                    var geminiBody = JsonSerializer.Serialize(new
                    {
                        content = new { parts = new[] { new { text } } }
                    });
                    var geminiResponse = await client.PostAsync(endpoint,
                        new StringContent(geminiBody, Encoding.UTF8, "application/json"), cancellationToken);
                    var geminiResponseBody = await geminiResponse.Content.ReadAsStringAsync(cancellationToken);

                    if (!geminiResponse.IsSuccessStatusCode) return null;

                    using var geminiDoc = JsonDocument.Parse(geminiResponseBody);
                    var values = geminiDoc.RootElement.GetProperty("embedding").GetProperty("values");
                    return values.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                }
                else
                {
                    var openaiBody = JsonSerializer.Serialize(new
                    {
                        model = "text-embedding-3-small",
                        input = text
                    });
                    var openaiResponse = await client.PostAsync(endpoint,
                        new StringContent(openaiBody, Encoding.UTF8, "application/json"), cancellationToken);
                    var openaiResponseBody = await openaiResponse.Content.ReadAsStringAsync(cancellationToken);

                    if (!openaiResponse.IsSuccessStatusCode) return null;

                    using var openaiDoc = JsonDocument.Parse(openaiResponseBody);
                    var embedding = openaiDoc.RootElement.GetProperty("data")[0].GetProperty("embedding");
                    return embedding.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                }
            }
            else // Ollama
            {
                var ollamaBody = JsonSerializer.Serialize(new
                {
                    model = "nomic-embed-text",
                    prompt = text
                });
                var ollamaResponse = await client.PostAsync($"{config.Endpoint}/api/embeddings",
                    new StringContent(ollamaBody, Encoding.UTF8, "application/json"), cancellationToken);
                var ollamaResponseBody = await ollamaResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!ollamaResponse.IsSuccessStatusCode) return null;

                using var ollamaDoc = JsonDocument.Parse(ollamaResponseBody);
                var embedding = ollamaDoc.RootElement.GetProperty("embedding");
                return embedding.EnumerateArray().Select(v => v.GetSingle()).ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            return null;
        }
    }

    /// <summary>
    /// Split content into overlapping chunks for better retrieval quality.
    /// </summary>
    private static List<string> ChunkContent(string content, int maxChunkSize = 1000, int overlap = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return chunks;

        // Split by paragraphs first
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > maxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());

                // Keep overlap from end of current chunk
                var overlapText = currentChunk.ToString();
                currentChunk.Clear();
                if (overlapText.Length > overlap)
                {
                    currentChunk.Append(overlapText.Substring(overlapText.Length - overlap));
                }
            }
            currentChunk.AppendLine(paragraph);
        }

        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString().Trim());

        return chunks;
    }
}
