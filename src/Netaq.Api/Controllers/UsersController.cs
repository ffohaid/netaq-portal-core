using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Interfaces;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
public class UsersController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UsersController(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FullNameAr.Contains(search) ||
                u.FullNameEn.Contains(search) ||
                u.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<Domain.Enums.OrganizationRole>(role, out var roleEnum))
        {
            query = query.Where(u => u.Role == roleEnum);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Enums.UserStatus>(status, out var statusEnum))
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

        if (!Enum.TryParse<Domain.Enums.OrganizationRole>(request.Role, out var newRole))
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

        if (!Enum.TryParse<Domain.Enums.UserStatus>(request.Status, out var newStatus))
            return BadRequest(ApiResponse<string>.Failure("Invalid status"));

        user.Status = newStatus;
        await _context.SaveChangesAsync(default);

        return Ok(ApiResponse<string>.Success("Status updated successfully"));
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

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
