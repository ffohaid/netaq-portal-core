using Netaq.Domain.Common;

namespace Netaq.Domain.Entities;

/// <summary>
/// Stores password reset tokens for the forgot-password flow.
/// Tokens are single-use and time-limited (30 minutes).
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Hashed token sent via email.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Token expiry time (30 minutes from creation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether the token has been used.
    /// </summary>
    public bool IsUsed { get; set; } = false;
    
    /// <summary>
    /// When the token was used.
    /// </summary>
    public DateTime? UsedAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
