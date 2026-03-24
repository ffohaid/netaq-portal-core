using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Encryption;
using Netaq.Infrastructure.Persistence;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

/// <summary>
/// System settings management: Organization branding, AI configuration, 
/// authentication settings, and general system parameters.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditTrailService _auditTrailService;

    public SettingsController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IEncryptionService encryptionService,
        IAuditTrailService auditTrailService)
    {
        _context = context;
        _currentUser = currentUser;
        _encryptionService = encryptionService;
        _auditTrailService = auditTrailService;
    }

    // ===== Organization Settings =====

    /// <summary>
    /// Get organization profile and branding settings.
    /// </summary>
    [HttpGet("organization")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> GetOrganizationSettings()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == _currentUser.OrganizationId.Value);

        if (org == null)
            return NotFound();

        var dto = new OrganizationSettingsDto
        {
            Id = org.Id,
            NameAr = org.NameAr,
            NameEn = org.NameEn,
            DescriptionAr = org.DescriptionAr,
            DescriptionEn = org.DescriptionEn,
            Address = org.Address,
            Phone = org.Phone,
            Email = org.Email,
            Website = org.Website,
            LogoUrl = org.LogoUrl,
            ShowPlatformLogo = org.ShowPlatformLogo,
            ActiveAuthProvider = org.ActiveAuthProvider,
            IsOtpEnabled = org.IsOtpEnabled,
            SsoEndpoint = org.SsoEndpoint,
            SsoClientId = org.SsoClientId,
            AdDomain = org.AdDomain,
            AdLdapUrl = org.AdLdapUrl
        };

        return Ok(ApiResponse<OrganizationSettingsDto>.Success(dto));
    }

    /// <summary>
    /// Update organization profile and branding.
    /// </summary>
    [HttpPut("organization")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateOrganizationSettings(
        [FromBody] UpdateOrganizationSettingsRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var org = await ((ApplicationDbContext)_context).Organizations
            .FirstOrDefaultAsync(o => o.Id == _currentUser.OrganizationId.Value);

        if (org == null)
            return NotFound();

        var oldValues = new
        {
            org.NameAr, org.NameEn, org.DescriptionAr, org.DescriptionEn,
            org.Address, org.Phone, org.Email, org.Website, org.LogoUrl, org.ShowPlatformLogo
        };

        org.NameAr = request.NameAr ?? org.NameAr;
        org.NameEn = request.NameEn ?? org.NameEn;
        org.DescriptionAr = request.DescriptionAr ?? org.DescriptionAr;
        org.DescriptionEn = request.DescriptionEn ?? org.DescriptionEn;
        org.Address = request.Address ?? org.Address;
        org.Phone = request.Phone ?? org.Phone;
        org.Email = request.Email ?? org.Email;
        org.Website = request.Website ?? org.Website;
        org.LogoUrl = request.LogoUrl ?? org.LogoUrl;
        org.ShowPlatformLogo = request.ShowPlatformLogo ?? org.ShowPlatformLogo;
        org.UpdatedAt = DateTime.UtcNow;
        org.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.OrganizationSettings,
            "OrganizationProfileUpdated",
            "Organization profile and branding settings updated",
            "Organization", org.Id,
            System.Text.Json.JsonSerializer.Serialize(oldValues),
            System.Text.Json.JsonSerializer.Serialize(request));

        return Ok(ApiResponse<bool>.Success(true));
    }

    // ===== Authentication Settings =====

    /// <summary>
    /// Update authentication provider settings.
    /// </summary>
    [HttpPut("authentication")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAuthenticationSettings(
        [FromBody] UpdateAuthSettingsRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var org = await ((ApplicationDbContext)_context).Organizations
            .FirstOrDefaultAsync(o => o.Id == _currentUser.OrganizationId.Value);

        if (org == null)
            return NotFound();

        org.ActiveAuthProvider = request.ActiveAuthProvider;
        org.IsOtpEnabled = request.IsOtpEnabled;

        if (request.ActiveAuthProvider == AuthProviderType.Nafath)
        {
            org.SsoEndpoint = request.SsoEndpoint;
            org.SsoClientId = request.SsoClientId;
            if (!string.IsNullOrEmpty(request.SsoClientSecret))
                org.SsoClientSecretEncrypted = _encryptionService.Encrypt(request.SsoClientSecret);
        }
        else if (request.ActiveAuthProvider == AuthProviderType.ActiveDirectory)
        {
            org.AdDomain = request.AdDomain;
            org.AdLdapUrl = request.AdLdapUrl;
        }

        org.UpdatedAt = DateTime.UtcNow;
        org.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.OrganizationSettings,
            "AuthenticationSettingsUpdated",
            $"Authentication provider changed to {request.ActiveAuthProvider}",
            "Organization", org.Id);

        return Ok(ApiResponse<bool>.Success(true));
    }

    // ===== AI Configuration =====

    /// <summary>
    /// Get all AI configurations for the organization.
    /// </summary>
    [HttpGet("ai-configurations")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<List<AiConfigurationDto>>>> GetAiConfigurations()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var configs = await _context.AiConfigurations
            .Where(c => c.OrganizationId == _currentUser.OrganizationId.Value)
            .Select(c => new AiConfigurationDto
            {
                Id = c.Id,
                ProviderType = c.ProviderType,
                ProviderName = c.ProviderName,
                IsActive = c.IsActive,
                Endpoint = c.Endpoint,
                ModelName = c.ModelName,
                HasApiKey = !string.IsNullOrEmpty(c.ApiKeyEncrypted),
                Temperature = c.Temperature,
                MaxTokens = c.MaxTokens,
                VectorDbEndpoint = c.VectorDbEndpoint,
                EmbeddingModel = c.EmbeddingModel,
                ChunkSize = c.ChunkSize,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AiConfigurationDto>>.Success(configs));
    }

    /// <summary>
    /// Create a new AI configuration.
    /// </summary>
    [HttpPost("ai-configurations")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAiConfiguration(
        [FromBody] CreateAiConfigurationRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        // Deactivate existing configs if this one is active
        if (request.IsActive)
        {
            var existingConfigs = await _context.AiConfigurations
                .Where(c => c.OrganizationId == _currentUser.OrganizationId.Value && c.IsActive)
                .ToListAsync();
            foreach (var config in existingConfigs)
                config.IsActive = false;
        }

        var aiConfig = new AiConfiguration
        {
            OrganizationId = _currentUser.OrganizationId.Value,
            ProviderType = request.ProviderType,
            ProviderName = request.ProviderName,
            IsActive = request.IsActive,
            Endpoint = request.Endpoint,
            ModelName = request.ModelName,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            VectorDbEndpoint = request.VectorDbEndpoint,
            EmbeddingModel = request.EmbeddingModel,
            ChunkSize = request.ChunkSize,
            CreatedBy = _currentUser.UserId
        };

        if (!string.IsNullOrEmpty(request.ApiKey))
            aiConfig.ApiKeyEncrypted = _encryptionService.Encrypt(request.ApiKey);

        _context.AiConfigurations.Add(aiConfig);
        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.AiConfiguration,
            "AiConfigurationCreated",
            $"AI configuration created: {request.ProviderName} ({request.ProviderType})",
            "AiConfiguration", aiConfig.Id);

        return Ok(ApiResponse<Guid>.Success(aiConfig.Id));
    }

    /// <summary>
    /// Update an existing AI configuration.
    /// </summary>
    [HttpPut("ai-configurations/{id}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAiConfiguration(
        Guid id, [FromBody] UpdateAiConfigurationRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var config = await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == _currentUser.OrganizationId.Value);

        if (config == null)
            return NotFound();

        // Deactivate others if activating this one
        if (request.IsActive && !config.IsActive)
        {
            var existingConfigs = await _context.AiConfigurations
                .Where(c => c.OrganizationId == _currentUser.OrganizationId.Value && c.IsActive && c.Id != id)
                .ToListAsync();
            foreach (var c in existingConfigs)
                c.IsActive = false;
        }

        config.ProviderType = request.ProviderType;
        config.ProviderName = request.ProviderName;
        config.IsActive = request.IsActive;
        config.Endpoint = request.Endpoint;
        config.ModelName = request.ModelName;
        config.Temperature = request.Temperature;
        config.MaxTokens = request.MaxTokens;
        config.VectorDbEndpoint = request.VectorDbEndpoint;
        config.EmbeddingModel = request.EmbeddingModel;
        config.ChunkSize = request.ChunkSize;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = _currentUser.UserId;

        if (!string.IsNullOrEmpty(request.ApiKey))
            config.ApiKeyEncrypted = _encryptionService.Encrypt(request.ApiKey);

        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.AiConfiguration,
            "AiConfigurationUpdated",
            $"AI configuration updated: {request.ProviderName}",
            "AiConfiguration", config.Id);

        return Ok(ApiResponse<bool>.Success(true));
    }

    /// <summary>
    /// Delete an AI configuration.
    /// </summary>
    [HttpDelete("ai-configurations/{id}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAiConfiguration(Guid id)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var config = await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == _currentUser.OrganizationId.Value);

        if (config == null)
            return NotFound();

        config.IsDeleted = true;
        config.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.AiConfiguration,
            "AiConfigurationDeleted",
            $"AI configuration deleted: {config.ProviderName}",
            "AiConfiguration", config.Id);

        return Ok(ApiResponse<bool>.Success(true));
    }

    /// <summary>
    /// Test AI configuration connectivity.
    /// </summary>
    [HttpPost("ai-configurations/{id}/test")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<AiTestResult>>> TestAiConfiguration(Guid id)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var config = await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == _currentUser.OrganizationId.Value);

        if (config == null)
            return NotFound();

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            
            // Simple connectivity test based on provider type
            string testUrl = config.ProviderType switch
            {
                AiProviderType.Ollama or AiProviderType.Aya => $"{config.Endpoint}/api/tags",
                AiProviderType.Gemini => $"{config.Endpoint}/v1beta/models?key=test",
                _ => config.Endpoint
            };

            var response = await httpClient.GetAsync(testUrl);
            
            return Ok(ApiResponse<AiTestResult>.Success(new AiTestResult
            {
                IsConnected = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Message = response.IsSuccessStatusCode 
                    ? "Connection successful" 
                    : $"Connection failed with status {response.StatusCode}"
            }));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<AiTestResult>.Success(new AiTestResult
            {
                IsConnected = false,
                StatusCode = 0,
                Message = $"Connection failed: {ex.Message}"
            }));
        }
    }

    // ===== Knowledge Base Management =====

    /// <summary>
    /// Get knowledge base sources and statistics.
    /// </summary>
    [HttpGet("knowledge-sources")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<KnowledgeSourceListDto>>> GetKnowledgeSources(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var query = _context.KnowledgeSources
            .Where(ks => ks.OrganizationId == _currentUser.OrganizationId.Value);

        var totalCount = await query.CountAsync();

        var sources = await query
            .OrderByDescending(ks => ks.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ks => new KnowledgeSourceDto
            {
                Id = ks.Id,
                SourceType = ks.SourceType,
                TitleAr = ks.TitleAr,
                TitleEn = ks.TitleEn,
                DescriptionAr = ks.DescriptionAr,
                DescriptionEn = ks.DescriptionEn,
                TenderId = ks.TenderId,
                FileUrl = ks.FileUrl,
                IndexingStatus = ks.IndexingStatus,
                TotalChunks = ks.TotalChunks,
                TotalVectors = ks.TotalVectors,
                LastIndexedAt = ks.LastIndexedAt,
                IndexingError = ks.IndexingError,
                CreatedAt = ks.CreatedAt
            })
            .ToListAsync();

        // Statistics
        var allSources = await query.ToListAsync();
        var stats = new KnowledgeBaseStatsDto
        {
            TotalSources = allSources.Count,
            AutoIndexedCount = allSources.Count(s => s.SourceType == KnowledgeSourceType.AutoIndexed),
            ManualUploadCount = allSources.Count(s => s.SourceType == KnowledgeSourceType.ManualUpload),
            IndexedCount = allSources.Count(s => s.IndexingStatus == KnowledgeIndexingStatus.Completed),
            PendingCount = allSources.Count(s => s.IndexingStatus == KnowledgeIndexingStatus.Pending),
            FailedCount = allSources.Count(s => s.IndexingStatus == KnowledgeIndexingStatus.Failed),
            TotalChunks = allSources.Sum(s => s.TotalChunks),
            TotalVectors = allSources.Sum(s => s.TotalVectors)
        };

        return Ok(ApiResponse<KnowledgeSourceListDto>.Success(new KnowledgeSourceListDto
        {
            Sources = new PaginatedResponse<KnowledgeSourceDto>
            {
                Items = sources,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            Statistics = stats
        }));
    }

    /// <summary>
    /// Manually add a knowledge source for indexing.
    /// </summary>
    [HttpPost("knowledge-sources")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddKnowledgeSource(
        [FromBody] AddKnowledgeSourceRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var source = new KnowledgeSource
        {
            OrganizationId = _currentUser.OrganizationId.Value,
            SourceType = KnowledgeSourceType.ManualUpload,
            TitleAr = request.TitleAr,
            TitleEn = request.TitleEn,
            DescriptionAr = request.DescriptionAr,
            DescriptionEn = request.DescriptionEn,
            FileUrl = request.FileUrl,
            ContentText = request.ContentText,
            IndexingStatus = KnowledgeIndexingStatus.Pending,
            CreatedBy = _currentUser.UserId
        };

        _context.KnowledgeSources.Add(source);
        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.AiConfiguration,
            "KnowledgeSourceAdded",
            $"Knowledge source added: {request.TitleEn}",
            "KnowledgeSource", source.Id);

        return Ok(ApiResponse<Guid>.Success(source.Id));
    }

    /// <summary>
    /// Delete a knowledge source and its vectors.
    /// </summary>
    [HttpDelete("knowledge-sources/{id}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteKnowledgeSource(Guid id)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var source = await _context.KnowledgeSources
            .FirstOrDefaultAsync(ks => ks.Id == id && ks.OrganizationId == _currentUser.OrganizationId.Value);

        if (source == null)
            return NotFound();

        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.AiConfiguration,
            "KnowledgeSourceDeleted",
            $"Knowledge source deleted: {source.TitleEn}",
            "KnowledgeSource", source.Id);

        return Ok(ApiResponse<bool>.Success(true));
    }

    // ===== System Settings (Key-Value) =====

    /// <summary>
    /// Get all system settings grouped by category.
    /// </summary>
    [HttpGet("system")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, List<SystemSettingDto>>>>> GetSystemSettings()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var settings = await _context.SystemSettings
            .Where(s => s.OrganizationId == _currentUser.OrganizationId.Value)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SettingKey)
            .Select(s => new SystemSettingDto
            {
                Id = s.Id,
                Category = s.Category,
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                LabelAr = s.LabelAr,
                LabelEn = s.LabelEn,
                DataType = s.DataType,
                IsEditable = s.IsEditable
            })
            .ToListAsync();

        var grouped = settings.GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Ok(ApiResponse<Dictionary<string, List<SystemSettingDto>>>.Success(grouped));
    }

    /// <summary>
    /// Update a system setting value.
    /// </summary>
    [HttpPut("system/{id}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSystemSetting(
        Guid id, [FromBody] UpdateSystemSettingRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == _currentUser.OrganizationId.Value);

        if (setting == null)
            return NotFound();

        if (!setting.IsEditable)
            return BadRequest(ApiResponse<bool>.Failure("This setting is not editable."));

        var oldValue = setting.SettingValue;
        setting.SettingValue = request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.OrganizationSettings,
            "SystemSettingUpdated",
            $"Setting '{setting.SettingKey}' updated from '{oldValue}' to '{request.Value}'",
            "SystemSetting", setting.Id);

        return Ok(ApiResponse<bool>.Success(true));
    }

    /// <summary>
    /// Bulk update system settings.
    /// </summary>
    [HttpPut("system/bulk")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkUpdateSystemSettings(
        [FromBody] List<BulkSettingUpdate> updates)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var settingIds = updates.Select(u => u.Id).ToList();
        var settings = await _context.SystemSettings
            .Where(s => settingIds.Contains(s.Id) && s.OrganizationId == _currentUser.OrganizationId.Value)
            .ToListAsync();

        foreach (var update in updates)
        {
            var setting = settings.FirstOrDefault(s => s.Id == update.Id);
            if (setting != null && setting.IsEditable)
            {
                setting.SettingValue = update.Value;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = _currentUser.UserId;
            }
        }

        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId,
            AuditActionCategory.OrganizationSettings,
            "SystemSettingsBulkUpdated",
            $"Bulk update of {updates.Count} system settings",
            "SystemSetting", null);

        return Ok(ApiResponse<bool>.Success(true));
    }
}

// ===== DTOs =====

public class OrganizationSettingsDto
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public bool ShowPlatformLogo { get; set; }
    public AuthProviderType ActiveAuthProvider { get; set; }
    public bool IsOtpEnabled { get; set; }
    public string? SsoEndpoint { get; set; }
    public string? SsoClientId { get; set; }
    public string? AdDomain { get; set; }
    public string? AdLdapUrl { get; set; }
}

public class UpdateOrganizationSettingsRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public bool? ShowPlatformLogo { get; set; }
}

public class UpdateAuthSettingsRequest
{
    public AuthProviderType ActiveAuthProvider { get; set; }
    public bool IsOtpEnabled { get; set; }
    public string? SsoEndpoint { get; set; }
    public string? SsoClientId { get; set; }
    public string? SsoClientSecret { get; set; }
    public string? AdDomain { get; set; }
    public string? AdLdapUrl { get; set; }
}

public class AiConfigurationDto
{
    public Guid Id { get; set; }
    public AiProviderType ProviderType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public string? VectorDbEndpoint { get; set; }
    public string? EmbeddingModel { get; set; }
    public int ChunkSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAiConfigurationRequest
{
    public AiProviderType ProviderType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public string? VectorDbEndpoint { get; set; }
    public string? EmbeddingModel { get; set; }
    public int ChunkSize { get; set; } = 512;
}

public class UpdateAiConfigurationRequest
{
    public AiProviderType ProviderType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public string? VectorDbEndpoint { get; set; }
    public string? EmbeddingModel { get; set; }
    public int ChunkSize { get; set; } = 512;
}

public class AiTestResult
{
    public bool IsConnected { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class KnowledgeSourceListDto
{
    public PaginatedResponse<KnowledgeSourceDto> Sources { get; set; } = new();
    public KnowledgeBaseStatsDto Statistics { get; set; } = new();
}

public class KnowledgeSourceDto
{
    public Guid Id { get; set; }
    public KnowledgeSourceType SourceType { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public Guid? TenderId { get; set; }
    public string? FileUrl { get; set; }
    public KnowledgeIndexingStatus IndexingStatus { get; set; }
    public int TotalChunks { get; set; }
    public int TotalVectors { get; set; }
    public DateTime? LastIndexedAt { get; set; }
    public string? IndexingError { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddKnowledgeSourceRequest
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? FileUrl { get; set; }
    public string? ContentText { get; set; }
}

public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? LabelAr { get; set; }
    public string? LabelEn { get; set; }
    public string DataType { get; set; } = "string";
    public bool IsEditable { get; set; }
}

public class UpdateSystemSettingRequest
{
    public string Value { get; set; } = string.Empty;
}

public class BulkSettingUpdate
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

