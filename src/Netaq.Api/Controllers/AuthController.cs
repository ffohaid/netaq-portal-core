using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Auth.Commands;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
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
        // Find user by stored refresh token
        // For security, we validate the refresh token from Redis
        var users = await _context.Users.IgnoreQueryFilters().Where(u => !u.IsDeleted).ToListAsync();
        
        foreach (var user in users)
        {
            var storedToken = await _cacheService.GetRefreshTokenAsync(user.Id);
            if (storedToken == request.RefreshToken)
            {
                var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
                var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
                
                // Rotate refresh token
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
}

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
