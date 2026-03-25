using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Permissions.Commands;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Permissions.Queries;

// ==================== Get Full Permission Matrix ====================
public record GetPermissionMatrixQuery() : IRequest<ApiResponse<List<PermissionMatrixGroupDto>>>;

public class GetPermissionMatrixQueryHandler : IRequestHandler<GetPermissionMatrixQuery, ApiResponse<List<PermissionMatrixGroupDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetPermissionMatrixQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<PermissionMatrixGroupDto>>> Handle(GetPermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.OrganizationId == null)
            return ApiResponse<List<PermissionMatrixGroupDto>>.Failure("Organization context required.");

        var orgId = _currentUser.OrganizationId.Value;

        var permissions = await _context.PermissionMatrices
            .Where(p => p.OrganizationId == orgId)
            .Include(p => p.User)
            .OrderBy(p => p.UserRole)
            .ThenBy(p => p.TenderPhase)
            .ToListAsync(cancellationToken);

        var grouped = permissions
            .GroupBy(p => p.UserRole)
            .Select(g => new PermissionMatrixGroupDto(
                g.Key,
                g.Key.ToString(),
                g.Select(p => new PermissionMatrixDto(
                    p.Id, p.UserId,
                    p.User?.FullNameAr, p.User?.FullNameEn, p.User?.Email,
                    p.UserRole, p.TenderPhase,
                    p.CanView, p.CanCreate, p.CanEdit, p.CanDelete,
                    p.CanApprove, p.CanReject, p.CanDelegate, p.CanExport
                )).ToList()
            ))
            .ToList();

        return ApiResponse<List<PermissionMatrixGroupDto>>.Success(grouped);
    }
}

// ==================== Check User Permission ====================
public record CheckPermissionQuery(
    TenderPhase Phase,
    string Permission // CanView, CanCreate, CanEdit, etc.
) : IRequest<ApiResponse<bool>>;

public class CheckPermissionQueryHandler : IRequestHandler<CheckPermissionQuery, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CheckPermissionQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<bool>> Handle(CheckPermissionQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.OrganizationId == null || _currentUser.UserId == null)
            return ApiResponse<bool>.Failure("Authentication required.");

        var orgId = _currentUser.OrganizationId.Value;
        var userRole = Enum.TryParse<OrganizationRole>(_currentUser.Role, out var role) ? role : OrganizationRole.Viewer;

        var permission = await _context.PermissionMatrices
            .FirstOrDefaultAsync(p =>
                p.OrganizationId == orgId &&
                p.UserRole == userRole &&
                p.TenderPhase == request.Phase,
                cancellationToken);

        if (permission == null)
            return ApiResponse<bool>.Success(false);

        var hasPermission = request.Permission.ToLower() switch
        {
            "canview" => permission.CanView,
            "cancreate" => permission.CanCreate,
            "canedit" => permission.CanEdit,
            "candelete" => permission.CanDelete,
            "canapprove" => permission.CanApprove,
            "canreject" => permission.CanReject,
            "candelegate" => permission.CanDelegate,
            "canexport" => permission.CanExport,
            _ => false
        };

        return ApiResponse<bool>.Success(hasPermission);
    }
}
