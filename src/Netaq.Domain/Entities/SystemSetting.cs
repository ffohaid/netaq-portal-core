using Netaq.Domain.Common;

namespace Netaq.Domain.Entities;

/// <summary>
/// Key-value system settings per organization. Used for branding, email templates,
/// SLA defaults, and other configurable parameters.
/// </summary>
public class SystemSetting : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Setting category for grouping (e.g., "Branding", "Email", "SLA", "General").
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique setting key within the category (e.g., "PrimaryColor", "DefaultSlaDuration").
    /// </summary>
    public string SettingKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Setting value stored as string (parsed by consumers).
    /// </summary>
    public string SettingValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Arabic label for display in settings UI.
    /// </summary>
    public string? LabelAr { get; set; }
    
    /// <summary>
    /// English label for display in settings UI.
    /// </summary>
    public string? LabelEn { get; set; }
    
    /// <summary>
    /// Data type hint for the UI (string, number, boolean, color, url, json).
    /// </summary>
    public string DataType { get; set; } = "string";
    
    /// <summary>
    /// Whether this setting is editable by organization admins.
    /// </summary>
    public bool IsEditable { get; set; } = true;
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
