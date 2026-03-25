using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Permissions.Commands;

// ==================== DTOs ====================
public record PermissionMatrixDto(
    Guid Id,
    Guid? UserId,
    string? UserFullNameAr,
    string? UserFullNameEn,
    string? UserEmail,
    OrganizationRole UserRole,
    TenderPhase TenderPhase,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete,
    bool CanApprove,
    bool CanReject,
    bool CanDelegate,
    bool CanExport
);

public record PermissionMatrixGroupDto(
    OrganizationRole Role,
    string RoleName,
    List<PermissionMatrixDto> Phases
);

// ==================== Update Permission Matrix (Batch) ====================
public record UpdatePermissionMatrixCommand(
    List<PermissionMatrixEntry> Entries
) : IRequest<ApiResponse<List<PermissionMatrixDto>>>;

public record PermissionMatrixEntry(
    OrganizationRole UserRole,
    TenderPhase TenderPhase,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete,
    bool CanApprove,
    bool CanReject,
    bool CanDelegate,
    bool CanExport
);

public class UpdatePermissionMatrixCommandValidator : AbstractValidator<UpdatePermissionMatrixCommand>
{
    public UpdatePermissionMatrixCommandValidator()
    {
        RuleFor(x => x.Entries).NotEmpty().WithMessage("At least one permission entry is required.");
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.UserRole).IsInEnum();
            entry.RuleFor(e => e.TenderPhase).IsInEnum();
        });
    }
}

public class UpdatePermissionMatrixCommandHandler : IRequestHandler<UpdatePermissionMatrixCommand, ApiResponse<List<PermissionMatrixDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdatePermissionMatrixCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<PermissionMatrixDto>>> Handle(UpdatePermissionMatrixCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.OrganizationId == null)
            return ApiResponse<List<PermissionMatrixDto>>.Failure("Organization context required.");

        var orgId = _currentUser.OrganizationId.Value;
        var result = new List<PermissionMatrixDto>();

        foreach (var entry in request.Entries)
        {
            var existing = await _context.PermissionMatrices
                .FirstOrDefaultAsync(p =>
                    p.OrganizationId == orgId &&
                    p.UserRole == entry.UserRole &&
                    p.TenderPhase == entry.TenderPhase,
                    cancellationToken);

            if (existing != null)
            {
                existing.CanView = entry.CanView;
                existing.CanCreate = entry.CanCreate;
                existing.CanEdit = entry.CanEdit;
                existing.CanDelete = entry.CanDelete;
                existing.CanApprove = entry.CanApprove;
                existing.CanReject = entry.CanReject;
                existing.CanDelegate = entry.CanDelegate;
                existing.CanExport = entry.CanExport;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = _currentUser.UserId;
            }
            else
            {
                existing = new PermissionMatrix
                {
                    OrganizationId = orgId,
                    UserRole = entry.UserRole,
                    TenderPhase = entry.TenderPhase,
                    CanView = entry.CanView,
                    CanCreate = entry.CanCreate,
                    CanEdit = entry.CanEdit,
                    CanDelete = entry.CanDelete,
                    CanApprove = entry.CanApprove,
                    CanReject = entry.CanReject,
                    CanDelegate = entry.CanDelegate,
                    CanExport = entry.CanExport,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId
                };
                _context.PermissionMatrices.Add(existing);
            }

            result.Add(new PermissionMatrixDto(
                existing.Id, existing.UserId, null, null, null,
                existing.UserRole, existing.TenderPhase,
                existing.CanView, existing.CanCreate, existing.CanEdit, existing.CanDelete,
                existing.CanApprove, existing.CanReject, existing.CanDelegate, existing.CanExport
            ));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<List<PermissionMatrixDto>>.Success(result, "Permission matrix updated successfully.");
    }
}
