using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Database-driven AI provider configuration. API keys encrypted with AES-256.
/// Supports dynamic switching between Cloud (Gemini) and On-Premise (Ollama/Aya).
/// </summary>
public class AiConfiguration : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    public AiProviderType ProviderType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    
    /// <summary>
    /// API endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Model name (e.g., "gemini-pro", "aya:latest").
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    
    /// <summary>
    /// AES-256 encrypted API key. Decrypted only in-memory during execution.
    /// </summary>
    public string? ApiKeyEncrypted { get; set; }
    
    /// <summary>
    /// Temperature setting for AI model.
    /// </summary>
    public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum tokens for response.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
    
    // RAG Settings
    public string? VectorDbEndpoint { get; set; }
    public string? EmbeddingModel { get; set; }
    public int ChunkSize { get; set; } = 512;
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
