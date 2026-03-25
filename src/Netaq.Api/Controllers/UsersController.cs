using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
public class UsersController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditTrailService _auditTrailService;
    private readonly IEmailService _emailService;

    public UsersController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditTrailService auditTrailService,
        IEmailService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditTrailService = auditTrailService;
        _emailService = emailService;
    }

    /// <summary>
    /// Get all users for the current organization.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserListResponse>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var query = _context.Users
            .Where(u => u.OrganizationId == _currentUser.OrganizationId.Value)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FullNameAr.Contains(search) ||
                u.FullNameEn.Contains(search) ||
                u.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<OrganizationRole>(role, out var roleEnum))
        {
            query = query.Where(u => u.Role == roleEnum);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
        {
            query = query.Where(u => u.Status == statusEnum);
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullNameAr = u.FullNameAr,
                FullNameEn = u.FullNameEn,
                Email = u.Email,
                Phone = u.Phone,
                JobTitleAr = u.JobTitleAr,
                JobTitleEn = u.JobTitleEn,
                DepartmentAr = u.DepartmentAr,
                DepartmentEn = u.DepartmentEn,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync();

        var response = new UserListResponse
        {
            Users = users,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        return Ok(ApiResponse<UserListResponse>.Success(response));
    }

    /// <summary>
    /// Get a single user's details.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid userId)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var user = await _context.Users
            .Where(u => u.Id == userId && u.OrganizationId == _currentUser.OrganizationId.Value)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullNameAr = u.FullNameAr,
                FullNameEn = u.FullNameEn,
                Email = u.Email,
                Phone = u.Phone,
                JobTitleAr = u.JobTitleAr,
                JobTitleEn = u.JobTitleEn,
                DepartmentAr = u.DepartmentAr,
                DepartmentEn = u.DepartmentEn,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(ApiResponse<UserDto>.Failure("User not found"));

        return Ok(ApiResponse<UserDto>.Success(user));
    }

    /// <summary>
    /// Update a user's profile data (admin action).
    /// </summary>
    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateUser(
        Guid userId,
        [FromBody] AdminUpdateUserRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == _currentUser.OrganizationId.Value);

        if (user == null)
            return NotFound(ApiResponse<string>.Failure("User not found"));

        if (!string.IsNullOrWhiteSpace(request.FullNameAr)) user.FullNameAr = request.FullNameAr;
        if (!string.IsNullOrWhiteSpace(request.FullNameEn)) user.FullNameEn = request.FullNameEn;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.JobTitleAr != null) user.JobTitleAr = request.JobTitleAr;
        if (request.JobTitleEn != null) user.JobTitleEn = request.JobTitleEn;
        if (request.DepartmentAr != null) user.DepartmentAr = request.DepartmentAr;
        if (request.DepartmentEn != null) user.DepartmentEn = request.DepartmentEn;
        if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<OrganizationRole>(request.Role, out var newRole))
            user.Role = newRole;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<UserStatus>(request.Status, out var newStatus))
            user.Status = newStatus;

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId.Value;
        await _context.SaveChangesAsync(default);

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
            AuditActionCategory.UserManagement, "USER_UPDATED",
            $"Admin updated user {user.Email}",
            "User", userId,
            ipAddress: _currentUser.IpAddress,
            userAgent: _currentUser.UserAgent);

        return Ok(ApiResponse<string>.Success("User updated successfully"));
    }

    /// <summary>
    /// Update a user's role.
    /// </summary>
    [HttpPut("{userId:guid}/role")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateUserRole(
        Guid userId,
        [FromBody] UpdateUserRoleRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == _currentUser.OrganizationId.Value);

        if (user == null)
            return NotFound(ApiResponse<string>.Failure("User not found"));

        if (!Enum.TryParse<OrganizationRole>(request.Role, out var newRole))
            return BadRequest(ApiResponse<string>.Failure("Invalid role"));

        user.Role = newRole;
        await _context.SaveChangesAsync(default);

        return Ok(ApiResponse<string>.Success("Role updated successfully"));
    }

    /// <summary>
    /// Update a user's status (activate/suspend/deactivate).
    /// </summary>
    [HttpPut("{userId:guid}/status")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateUserStatus(
        Guid userId,
        [FromBody] UpdateUserStatusRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == _currentUser.OrganizationId.Value);

        if (user == null)
            return NotFound(ApiResponse<string>.Failure("User not found"));

        if (!Enum.TryParse<UserStatus>(request.Status, out var newStatus))
            return BadRequest(ApiResponse<string>.Failure("Invalid status"));

        user.Status = newStatus;
        await _context.SaveChangesAsync(default);

        return Ok(ApiResponse<string>.Success("Status updated successfully"));
    }

    /// <summary>
    /// Admin reset a user's password and send temporary password via email.
    /// </summary>
    [HttpPost("{userId:guid}/reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> AdminResetPassword(Guid userId)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == _currentUser.OrganizationId.Value);

        if (user == null)
            return NotFound(ApiResponse<string>.Failure("User not found"));

        // Generate temporary password
        var tempPassword = GenerateTemporaryPassword();
        var (hash, salt) = HashPassword(tempPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId.Value;
        await _context.SaveChangesAsync(default);

        // Send email with temporary password
        await _emailService.SendAsync(
            user.Email,
            "إعادة تعيين كلمة المرور - منصة نِطاق | Password Reset - NETAQ Portal",
            $@"<div dir='rtl' style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>إعادة تعيين كلمة المرور</h2>
                <p>مرحباً {user.FullNameAr}،</p>
                <p>تم إعادة تعيين كلمة المرور الخاصة بك بواسطة مدير النظام.</p>
                <p>كلمة المرور المؤقتة:</p>
                <div style='font-size: 20px; font-weight: bold; color: #1a56db; padding: 16px; background: #f0f4ff; border-radius: 8px; text-align: center; letter-spacing: 2px;'>
                    {tempPassword}
                </div>
                <p style='color: #dc2626;'>يرجى تغيير كلمة المرور فور تسجيل الدخول.</p>
                <hr/>
                <h2 dir='ltr'>Password Reset</h2>
                <p dir='ltr'>Hello {user.FullNameEn},</p>
                <p dir='ltr'>Your password has been reset by the system administrator.</p>
                <p dir='ltr'>Temporary password:</p>
                <div style='font-size: 20px; font-weight: bold; color: #1a56db; padding: 16px; background: #f0f4ff; border-radius: 8px; text-align: center; letter-spacing: 2px;'>
                    {tempPassword}
                </div>
                <p dir='ltr' style='color: #dc2626;'>Please change your password immediately after logging in.</p>
            </div>");

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
            AuditActionCategory.UserManagement, "ADMIN_PASSWORD_RESET",
            $"Admin reset password for user {user.Email}",
            "User", userId,
            ipAddress: _currentUser.IpAddress,
            userAgent: _currentUser.UserAgent);

        return Ok(ApiResponse<string>.Success("Password reset successfully. Temporary password sent via email."));
    }

    /// <summary>
    /// Soft-delete a user (admin action).
    /// </summary>
    [HttpDelete("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteUser(Guid userId)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        // Prevent self-deletion
        if (userId == _currentUser.UserId.Value)
            return BadRequest(ApiResponse<string>.Failure("Cannot delete your own account."));

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == _currentUser.OrganizationId.Value);

        if (user == null)
            return NotFound(ApiResponse<string>.Failure("User not found"));

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserStatus.Deactivated;
        await _context.SaveChangesAsync(default);

        await _auditTrailService.LogAsync(
            _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
            AuditActionCategory.UserManagement, "USER_DELETED",
            $"Admin deleted user {user.Email}",
            "User", userId,
            ipAddress: _currentUser.IpAddress,
            userAgent: _currentUser.UserAgent);

        return Ok(ApiResponse<string>.Success("User deleted successfully"));
    }

    // ===== Helper Methods =====

    private static (string Hash, string Salt) HashPassword(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = Convert.ToBase64String(hmac.Key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (hash, salt);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var bytes = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}

// DTOs
public class UserListResponse
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? JobTitleAr { get; set; }
    public string? JobTitleEn { get; set; }
    public string? DepartmentAr { get; set; }
    public string? DepartmentEn { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUpdateUserRequest
{
    public string? FullNameAr { get; set; }
    public string? FullNameEn { get; set; }
    public string? Phone { get; set; }
    public string? JobTitleAr { get; set; }
    public string? JobTitleEn { get; set; }
    public string? DepartmentAr { get; set; }
    public string? DepartmentEn { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
}

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
