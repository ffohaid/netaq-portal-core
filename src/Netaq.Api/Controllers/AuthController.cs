using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Auth.Commands;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Cache;
using Netaq.Infrastructure.Identity;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICacheService _cacheService;
    private readonly IApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrailService;
    private readonly IEmailService _emailService;

    public AuthController(
        IMediator mediator,
        IJwtTokenService jwtTokenService,
        ICacheService cacheService,
        IApplicationDbContext context,
        IAuditTrailService auditTrailService,
        IEmailService emailService)
    {
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
        _cacheService = cacheService;
        _context = context;
        _auditTrailService = auditTrailService;
        _emailService = emailService;
    }

    /// <summary>
    /// Login with email and password. May require OTP verification.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result);

        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (user == null)
            return BadRequest(ApiResponse<LoginResponse>.Failure("User not found."));

        // If OTP is required, send OTP email
        if (result.Data!.RequiresOtp)
        {
            if (user.OtpCode != null)
            {
                await _emailService.SendOtpAsync(user.Email, user.OtpCode);
            }

            await _auditTrailService.LogAsync(
                user.OrganizationId, user.Id,
                AuditActionCategory.Authentication, "LOGIN_OTP_SENT",
                $"OTP sent to {user.Email}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Ok(result);
        }

        // No OTP - generate tokens directly
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        
        // Store refresh token in Redis (8 hours)
        await _cacheService.SetRefreshTokenAsync(user.Id, refreshToken, TimeSpan.FromHours(8));
        
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            user.OrganizationId, user.Id,
            AuditActionCategory.Authentication, "LOGIN_SUCCESS",
            $"User {user.Email} logged in successfully",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Ok(ApiResponse<LoginResponse>.Success(new LoginResponse(
            RequiresOtp: false,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            Message: "Login successful."
        )));
    }

    /// <summary>
    /// Verify OTP code and complete login.
    /// </summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var command = new VerifyOtpCommand(request.Email, request.OtpCode);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result);

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (user == null)
            return BadRequest(ApiResponse<TokenResponse>.Failure("User not found."));

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        
        await _cacheService.SetRefreshTokenAsync(user.Id, refreshToken, TimeSpan.FromHours(8));

        await _auditTrailService.LogAsync(
            user.OrganizationId, user.Id,
            AuditActionCategory.Authentication, "OTP_VERIFIED",
            $"OTP verified for {user.Email}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Ok(ApiResponse<TokenResponse>.Success(new TokenResponse(
            accessToken, refreshToken, DateTime.UtcNow.AddMinutes(60)
        )));
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var users = await _context.Users.IgnoreQueryFilters().Where(u => !u.IsDeleted).ToListAsync();
        
        foreach (var user in users)
        {
            var storedToken = await _cacheService.GetRefreshTokenAsync(user.Id);
            if (storedToken == request.RefreshToken)
            {
                var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
                var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
                
                await _cacheService.RemoveRefreshTokenAsync(user.Id);
                await _cacheService.SetRefreshTokenAsync(user.Id, newRefreshToken, TimeSpan.FromHours(8));

                return Ok(ApiResponse<TokenResponse>.Success(new TokenResponse(
                    newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(60)
                )));
            }
        }

        return Unauthorized(ApiResponse<TokenResponse>.Failure("Invalid refresh token."));
    }

    /// <summary>
    /// Logout and invalidate refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        var currentUser = HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (currentUser.UserId.HasValue)
        {
            await _cacheService.RemoveRefreshTokenAsync(currentUser.UserId.Value);
            
            await _auditTrailService.LogAsync(
                currentUser.OrganizationId!.Value, currentUser.UserId.Value,
                AuditActionCategory.Authentication, "LOGOUT",
                $"User {currentUser.Email} logged out",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Ok(ApiResponse<bool>.Success(true));
    }

    /// <summary>
    /// Get current user profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var currentUser = HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (!currentUser.UserId.HasValue)
            return Unauthorized(ApiResponse<UserProfileDto>.Failure("Not authenticated."));

        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId.Value);

        if (user == null)
            return NotFound(ApiResponse<UserProfileDto>.Failure("User not found."));

        var profile = new UserProfileDto(
            user.Id,
            user.FullNameAr,
            user.FullNameEn,
            user.Email,
            user.Phone,
            user.JobTitleAr,
            user.JobTitleEn,
            user.DepartmentAr,
            user.DepartmentEn,
            user.Role.ToString(),
            user.Locale,
            user.AvatarUrl,
            user.Organization.NameAr,
            user.Organization.NameEn,
            user.Organization.LogoUrl,
            user.LastLoginAt);

        return Ok(ApiResponse<UserProfileDto>.Success(profile));
    }

    /// <summary>
    /// Update current user profile (name, phone, job title, department, locale).
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var currentUser = HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (!currentUser.UserId.HasValue)
            return Unauthorized(ApiResponse<UserProfileDto>.Failure("Not authenticated."));

        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId.Value);

        if (user == null)
            return NotFound(ApiResponse<UserProfileDto>.Failure("User not found."));

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.FullNameAr)) user.FullNameAr = request.FullNameAr;
        if (!string.IsNullOrWhiteSpace(request.FullNameEn)) user.FullNameEn = request.FullNameEn;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.JobTitleAr != null) user.JobTitleAr = request.JobTitleAr;
        if (request.JobTitleEn != null) user.JobTitleEn = request.JobTitleEn;
        if (request.DepartmentAr != null) user.DepartmentAr = request.DepartmentAr;
        if (request.DepartmentEn != null) user.DepartmentEn = request.DepartmentEn;
        if (!string.IsNullOrWhiteSpace(request.Locale)) user.Locale = request.Locale;
        
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser.UserId.Value;
        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            user.OrganizationId, user.Id,
            AuditActionCategory.UserManagement, "PROFILE_UPDATED",
            $"User {user.Email} updated their profile",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        var profile = new UserProfileDto(
            user.Id, user.FullNameAr, user.FullNameEn, user.Email, user.Phone,
            user.JobTitleAr, user.JobTitleEn, user.DepartmentAr, user.DepartmentEn,
            user.Role.ToString(), user.Locale, user.AvatarUrl,
            user.Organization.NameAr, user.Organization.NameEn, user.Organization.LogoUrl,
            user.LastLoginAt);

        return Ok(ApiResponse<UserProfileDto>.Success(profile));
    }

    /// <summary>
    /// Change current user's password (requires current password).
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var currentUser = HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (!currentUser.UserId.HasValue)
            return Unauthorized(ApiResponse<string>.Failure("Not authenticated."));

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId.Value && !u.IsDeleted);

        if (user == null)
            return NotFound(ApiResponse<string>.Failure("User not found."));

        // Verify current password
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash!, user.PasswordSalt!))
            return BadRequest(ApiResponse<string>.Failure("Current password is incorrect."));

        // Validate new password
        if (request.NewPassword.Length < 8)
            return BadRequest(ApiResponse<string>.Failure("New password must be at least 8 characters."));

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(ApiResponse<string>.Failure("New password and confirmation do not match."));

        // Hash new password
        var (hash, salt) = HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser.UserId.Value;
        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            user.OrganizationId, user.Id,
            AuditActionCategory.Authentication, "PASSWORD_CHANGED",
            $"User {user.Email} changed their password",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Ok(ApiResponse<string>.Success("Password changed successfully."));
    }

    /// <summary>
    /// Request a password reset link via email (forgot password flow).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Always return success to prevent email enumeration
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (user == null || user.Status != UserStatus.Active)
        {
            // Return success even if user not found (security best practice)
            return Ok(ApiResponse<string>.Success("If the email exists, a reset link has been sent."));
        }

        // Generate a secure reset token
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        // Invalidate any existing tokens for this user
        var existingTokens = await _context.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == user.Id && !t.IsUsed && !t.IsDeleted)
            .ToListAsync();

        foreach (var t in existingTokens)
        {
            t.IsDeleted = true;
            t.DeletedAt = DateTime.UtcNow;
        }

        // Create new token (valid for 30 minutes)
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedBy = user.Id
        };
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        // Send reset email
        await _emailService.SendAsync(
            user.Email,
            "إعادة تعيين كلمة المرور - منصة نِطاق | Password Reset - NETAQ Portal",
            GenerateResetEmailHtml(rawToken, user.FullNameAr));

        await _auditTrailService.LogAsync(
            user.OrganizationId, user.Id,
            AuditActionCategory.Authentication, "PASSWORD_RESET_REQUESTED",
            $"Password reset requested for {user.Email}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Ok(ApiResponse<string>.Success("If the email exists, a reset link has been sent."));
    }

    /// <summary>
    /// Validate a password reset token.
    /// </summary>
    [HttpGet("validate-reset-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateResetToken([FromQuery] string token)
    {
        var tokenHash = HashToken(token);
        var resetToken = await _context.PasswordResetTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsUsed && !t.IsDeleted);

        if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
            return Ok(ApiResponse<bool>.Failure("Invalid or expired reset token."));

        return Ok(ApiResponse<bool>.Success(true));
    }

    /// <summary>
    /// Reset password using a valid reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var tokenHash = HashToken(request.Token);
        var resetToken = await _context.PasswordResetTokens
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsUsed && !t.IsDeleted);

        if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
            return BadRequest(ApiResponse<string>.Failure("Invalid or expired reset token."));

        if (request.NewPassword.Length < 8)
            return BadRequest(ApiResponse<string>.Failure("Password must be at least 8 characters."));

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(ApiResponse<string>.Failure("Password and confirmation do not match."));

        var user = resetToken.User;
        var (hash, salt) = HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAt = DateTime.UtcNow;

        // Mark token as used
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditTrailService.LogAsync(
            user.OrganizationId, user.Id,
            AuditActionCategory.Authentication, "PASSWORD_RESET_COMPLETED",
            $"Password reset completed for {user.Email}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Ok(ApiResponse<string>.Success("Password has been reset successfully."));
    }

    // ===== Helper Methods =====

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        using var hmac = new HMACSHA512(saltBytes);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(computedHash) == storedHash;
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = Convert.ToBase64String(hmac.Key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (hash, salt);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateResetEmailHtml(string token, string userName)
    {
        var resetUrl = $"https://netaq.pro/auth/reset-password?token={Uri.EscapeDataString(token)}";
        return $@"
        <div dir='rtl' style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; margin: 0 auto;'>
            <div style='background: linear-gradient(135deg, #1e40af, #7c3aed); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
                <h1 style='color: white; margin: 0; font-size: 24px;'>منصة نِطاق</h1>
                <p style='color: #e0e7ff; margin: 8px 0 0;'>NETAQ Portal</p>
            </div>
            <div style='background: #ffffff; padding: 30px; border: 1px solid #e5e7eb; border-top: none;'>
                <h2 style='color: #1f2937; margin-top: 0;'>إعادة تعيين كلمة المرور</h2>
                <p style='color: #4b5563;'>مرحباً {userName}،</p>
                <p style='color: #4b5563;'>تم طلب إعادة تعيين كلمة المرور لحسابك. اضغط على الزر أدناه لإعادة التعيين:</p>
                <div style='text-align: center; margin: 24px 0;'>
                    <a href='{resetUrl}' 
                       style='background: linear-gradient(135deg, #1e40af, #7c3aed); color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: bold; font-size: 16px;'>
                        إعادة تعيين كلمة المرور
                    </a>
                </div>
                <p style='color: #9ca3af; font-size: 14px;'>هذا الرابط صالح لمدة 30 دقيقة فقط. إذا لم تطلب إعادة التعيين، يرجى تجاهل هذا البريد.</p>
                <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;'/>
                <h2 dir='ltr' style='color: #1f2937;'>Password Reset</h2>
                <p dir='ltr' style='color: #4b5563;'>Hello {userName},</p>
                <p dir='ltr' style='color: #4b5563;'>A password reset was requested for your account. Click the button below to reset:</p>
                <div style='text-align: center; margin: 24px 0;'>
                    <a href='{resetUrl}' 
                       style='background: linear-gradient(135deg, #1e40af, #7c3aed); color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: bold; font-size: 16px;'>
                        Reset Password
                    </a>
                </div>
                <p dir='ltr' style='color: #9ca3af; font-size: 14px;'>This link is valid for 30 minutes only. If you did not request this, please ignore this email.</p>
            </div>
            <div style='background: #f9fafb; padding: 16px; border-radius: 0 0 12px 12px; text-align: center; border: 1px solid #e5e7eb; border-top: none;'>
                <p style='color: #9ca3af; font-size: 12px; margin: 0;'>© 2026 منصة نِطاق - NETAQ Portal. جميع الحقوق محفوظة.</p>
            </div>
        </div>";
    }
}

// ===== DTOs =====

public record UserProfileDto(
    Guid Id,
    string FullNameAr,
    string FullNameEn,
    string Email,
    string? Phone,
    string? JobTitleAr,
    string? JobTitleEn,
    string? DepartmentAr,
    string? DepartmentEn,
    string Role,
    string Locale,
    string? AvatarUrl,
    string OrganizationNameAr,
    string OrganizationNameEn,
    string? OrganizationLogoUrl,
    DateTime? LastLoginAt);

public record UpdateProfileRequest(
    string? FullNameAr,
    string? FullNameEn,
    string? Phone,
    string? JobTitleAr,
    string? JobTitleEn,
    string? DepartmentAr,
    string? DepartmentEn,
    string? Locale);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmNewPassword);
