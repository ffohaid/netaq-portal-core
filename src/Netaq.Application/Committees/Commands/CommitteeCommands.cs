using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Netaq.Application.Committees.Commands;

// ==================== DTOs ====================
public record CommitteeDto(
    Guid Id,
    string NameAr,
    string NameEn,
    CommitteeType Type,
    string? PurposeAr,
    string? PurposeEn,
    bool IsActive,
    Guid? TenderId,
    string? TenderTitleAr,
    string? TenderTitleEn,
    DateTime? FormedAt,
    DateTime? DissolvedAt,
    DateTime? StartDate,
    DateTime? EndDate,
    string? FormationDecisionUrl,
    string? FormationDecisionNumber,
    DateTime CreatedAt,
    int MemberCount,
    List<CommitteeMemberDto>? Members = null
);

public record CommitteeMemberDto(
    Guid Id,
    Guid UserId,
    string UserFullNameAr,
    string UserFullNameEn,
    string UserEmail,
    CommitteeMemberRole Role,
    bool IsActive,
    DateTime JoinedAt,
    DateTime? LeftAt
);

// ==================== Create Committee ====================
public record CreateCommitteeCommand(
    string NameAr,
    string NameEn,
    CommitteeType Type,
    string? PurposeAr,
    string? PurposeEn,
    Guid? TenderId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? FormationDecisionNumber
) : IRequest<ApiResponse<CommitteeDto>>;

public class CreateCommitteeCommandValidator : AbstractValidator<CreateCommitteeCommand>
{
    public CreateCommitteeCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TenderId).NotNull()
            .When(x => x.Type == CommitteeType.Temporary)
            .WithMessage("Temporary committees must be linked to a tender.");
    }
}

public class CreateCommitteeCommandHandler : IRequestHandler<CreateCommitteeCommand, ApiResponse<CommitteeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateCommitteeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<CommitteeDto>> Handle(CreateCommitteeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.OrganizationId == null)
            return ApiResponse<CommitteeDto>.Failure("Organization context required.");

        // Validate tender exists if temporary
        if (request.Type == CommitteeType.Temporary && request.TenderId.HasValue)
        {
            var tenderExists = await _context.Tenders.AnyAsync(t => t.Id == request.TenderId.Value, cancellationToken);
            if (!tenderExists)
                return ApiResponse<CommitteeDto>.Failure("Tender not found.");
        }

        var committee = new Committee
        {
            OrganizationId = _currentUser.OrganizationId.Value,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Type = request.Type,
            PurposeAr = request.PurposeAr,
            PurposeEn = request.PurposeEn,
            TenderId = request.Type == CommitteeType.Temporary ? request.TenderId : null,
            IsActive = true,
            FormedAt = DateTime.UtcNow,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            FormationDecisionNumber = request.FormationDecisionNumber,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _context.Committees.Add(committee);

        // Audit Trail
        await LogAuditAsync(_context, _currentUser, "CommitteeCreated",
            $"Committee '{request.NameEn}' created", "Committee", committee.Id, null, committee, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new CommitteeDto(
            committee.Id, committee.NameAr, committee.NameEn, committee.Type,
            committee.PurposeAr, committee.PurposeEn, committee.IsActive,
            committee.TenderId, null, null, committee.FormedAt, committee.DissolvedAt,
            committee.StartDate, committee.EndDate, committee.FormationDecisionUrl,
            committee.FormationDecisionNumber, committee.CreatedAt, 0
        );

        return ApiResponse<CommitteeDto>.Success(dto, "Committee created successfully.");
    }

    internal static async Task LogAuditAsync(
        IApplicationDbContext context, ICurrentUserService currentUser,
        string actionType, string description, string entityType, Guid entityId,
        object? oldValues, object? newValues, CancellationToken ct)
    {
        var lastLog = await context.AuditLogs
            .OrderByDescending(a => a.SequenceNumber)
            .FirstOrDefaultAsync(ct);

        var seqNum = (lastLog?.SequenceNumber ?? 0) + 1;
        var previousHash = lastLog?.Hash ?? "GENESIS";

        var entry = new AuditLog
        {
            OrganizationId = currentUser.OrganizationId ?? Guid.Empty,
            UserId = currentUser.UserId,
            ActionCategory = AuditActionCategory.WorkflowAction,
            ActionType = actionType,
            ActionDescription = description,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = seqNum,
            PreviousHash = previousHash
        };

        // Compute SHA-256 hash
        var hashInput = $"{entry.ActionType}|{entry.EntityId}|{entry.UserId}|{entry.Timestamp:O}|{previousHash}";
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        entry.Hash = Convert.ToBase64String(hashBytes);

        context.AuditLogs.Add(entry);
    }
}

// ==================== Update Committee ====================
public record UpdateCommitteeCommand(
    Guid CommitteeId,
    string NameAr,
    string NameEn,
    string? PurposeAr,
    string? PurposeEn,
    DateTime? StartDate,
    DateTime? EndDate,
    string? FormationDecisionNumber
) : IRequest<ApiResponse<CommitteeDto>>;

public class UpdateCommitteeCommandValidator : AbstractValidator<UpdateCommitteeCommand>
{
    public UpdateCommitteeCommandValidator()
    {
        RuleFor(x => x.CommitteeId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
    }
}

public class UpdateCommitteeCommandHandler : IRequestHandler<UpdateCommitteeCommand, ApiResponse<CommitteeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateCommitteeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<CommitteeDto>> Handle(UpdateCommitteeCommand request, CancellationToken cancellationToken)
    {
        var committee = await _context.Committees
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.CommitteeId, cancellationToken);

        if (committee == null)
            return ApiResponse<CommitteeDto>.Failure("Committee not found.");

        var oldValues = new { committee.NameAr, committee.NameEn, committee.PurposeAr, committee.PurposeEn };

        committee.NameAr = request.NameAr;
        committee.NameEn = request.NameEn;
        committee.PurposeAr = request.PurposeAr;
        committee.PurposeEn = request.PurposeEn;
        committee.StartDate = request.StartDate;
        committee.EndDate = request.EndDate;
        committee.FormationDecisionNumber = request.FormationDecisionNumber;
        committee.UpdatedAt = DateTime.UtcNow;
        committee.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "CommitteeUpdated",
            $"Committee '{request.NameEn}' updated", "Committee", committee.Id, oldValues, committee, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new CommitteeDto(
            committee.Id, committee.NameAr, committee.NameEn, committee.Type,
            committee.PurposeAr, committee.PurposeEn, committee.IsActive,
            committee.TenderId, null, null, committee.FormedAt, committee.DissolvedAt,
            committee.StartDate, committee.EndDate, committee.FormationDecisionUrl,
            committee.FormationDecisionNumber, committee.CreatedAt,
            committee.Members.Count(m => m.IsActive)
        );

        return ApiResponse<CommitteeDto>.Success(dto, "Committee updated successfully.");
    }
}

// ==================== Dissolve Committee (Soft Delete) ====================
public record DissolveCommitteeCommand(Guid CommitteeId) : IRequest<ApiResponse<CommitteeDto>>;

public class DissolveCommitteeCommandHandler : IRequestHandler<DissolveCommitteeCommand, ApiResponse<CommitteeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DissolveCommitteeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<CommitteeDto>> Handle(DissolveCommitteeCommand request, CancellationToken cancellationToken)
    {
        var committee = await _context.Committees
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.CommitteeId, cancellationToken);

        if (committee == null)
            return ApiResponse<CommitteeDto>.Failure("Committee not found.");

        committee.IsActive = false;
        committee.DissolvedAt = DateTime.UtcNow;
        committee.IsDeleted = true;
        committee.DeletedAt = DateTime.UtcNow;
        committee.UpdatedAt = DateTime.UtcNow;
        committee.UpdatedBy = _currentUser.UserId;

        // Deactivate all members
        foreach (var member in committee.Members.Where(m => m.IsActive))
        {
            member.IsActive = false;
            member.LeftAt = DateTime.UtcNow;
        }

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "CommitteeDissolved",
            $"Committee '{committee.NameEn}' dissolved", "Committee", committee.Id, null, committee, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new CommitteeDto(
            committee.Id, committee.NameAr, committee.NameEn, committee.Type,
            committee.PurposeAr, committee.PurposeEn, committee.IsActive,
            committee.TenderId, null, null, committee.FormedAt, committee.DissolvedAt,
            committee.StartDate, committee.EndDate,
            committee.FormationDecisionUrl, committee.FormationDecisionNumber,
            committee.CreatedAt, 0
        );

        return ApiResponse<CommitteeDto>.Success(dto, "Committee dissolved successfully.");
    }
}

// ==================== Add Member ====================
public record AddCommitteeMemberCommand(
    Guid CommitteeId,
    Guid UserId,
    CommitteeMemberRole Role
) : IRequest<ApiResponse<CommitteeMemberDto>>;

public class AddCommitteeMemberCommandValidator : AbstractValidator<AddCommitteeMemberCommand>
{
    public AddCommitteeMemberCommandValidator()
    {
        RuleFor(x => x.CommitteeId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class AddCommitteeMemberCommandHandler : IRequestHandler<AddCommitteeMemberCommand, ApiResponse<CommitteeMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddCommitteeMemberCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<CommitteeMemberDto>> Handle(AddCommitteeMemberCommand request, CancellationToken cancellationToken)
    {
        var committee = await _context.Committees.FindAsync(new object[] { request.CommitteeId }, cancellationToken);
        if (committee == null || !committee.IsActive)
            return ApiResponse<CommitteeMemberDto>.Failure("Committee not found or inactive.");

        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null)
            return ApiResponse<CommitteeMemberDto>.Failure("User not found.");

        // Check if user is already an active member
        var existingMember = await _context.CommitteeMembers
            .AnyAsync(m => m.CommitteeId == request.CommitteeId && m.UserId == request.UserId && m.IsActive, cancellationToken);
        if (existingMember)
            return ApiResponse<CommitteeMemberDto>.Failure("User is already an active member of this committee.");

        // Only one Chair allowed
        if (request.Role == CommitteeMemberRole.Chair)
        {
            var existingChair = await _context.CommitteeMembers
                .AnyAsync(m => m.CommitteeId == request.CommitteeId && m.Role == CommitteeMemberRole.Chair && m.IsActive, cancellationToken);
            if (existingChair)
                return ApiResponse<CommitteeMemberDto>.Failure("Committee already has an active chair.");
        }

        var member = new CommitteeMember
        {
            CommitteeId = request.CommitteeId,
            UserId = request.UserId,
            Role = request.Role,
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _context.CommitteeMembers.Add(member);

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "CommitteeMemberAdded",
            $"User '{user.Email}' added to committee '{committee.NameEn}' as {request.Role}",
            "CommitteeMember", member.Id, null, member, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new CommitteeMemberDto(
            member.Id, member.UserId, user.FullNameAr, user.FullNameEn,
            user.Email, member.Role, member.IsActive, member.JoinedAt, member.LeftAt
        );

        return ApiResponse<CommitteeMemberDto>.Success(dto, "Member added successfully.");
    }
}

// ==================== Remove Member ====================
public record RemoveCommitteeMemberCommand(Guid CommitteeId, Guid MemberId) : IRequest<ApiResponse<bool>>;

public class RemoveCommitteeMemberCommandHandler : IRequestHandler<RemoveCommitteeMemberCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RemoveCommitteeMemberCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<bool>> Handle(RemoveCommitteeMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _context.CommitteeMembers
            .Include(m => m.User)
            .Include(m => m.Committee)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.CommitteeId == request.CommitteeId, cancellationToken);

        if (member == null)
            return ApiResponse<bool>.Failure("Member not found.");

        member.IsActive = false;
        member.LeftAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
        member.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "CommitteeMemberRemoved",
            $"User '{member.User.Email}' removed from committee '{member.Committee.NameEn}'",
            "CommitteeMember", member.Id, null, member, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true, "Member removed successfully.");
    }
}

// ==================== Change Member Role ====================
public record ChangeMemberRoleCommand(
    Guid CommitteeId,
    Guid MemberId,
    CommitteeMemberRole NewRole
) : IRequest<ApiResponse<CommitteeMemberDto>>;

public class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.CommitteeId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}

public class ChangeMemberRoleCommandHandler : IRequestHandler<ChangeMemberRoleCommand, ApiResponse<CommitteeMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ChangeMemberRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<CommitteeMemberDto>> Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var member = await _context.CommitteeMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.CommitteeId == request.CommitteeId && m.IsActive, cancellationToken);

        if (member == null)
            return ApiResponse<CommitteeMemberDto>.Failure("Active member not found.");

        // Only one Chair allowed
        if (request.NewRole == CommitteeMemberRole.Chair)
        {
            var existingChair = await _context.CommitteeMembers
                .AnyAsync(m => m.CommitteeId == request.CommitteeId && m.Role == CommitteeMemberRole.Chair
                    && m.IsActive && m.Id != request.MemberId, cancellationToken);
            if (existingChair)
                return ApiResponse<CommitteeMemberDto>.Failure("Committee already has an active chair. Remove existing chair first.");
        }

        var oldRole = member.Role;
        member.Role = request.NewRole;
        member.UpdatedAt = DateTime.UtcNow;
        member.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "CommitteeMemberRoleChanged",
            $"Member role changed from {oldRole} to {request.NewRole}",
            "CommitteeMember", member.Id, new { OldRole = oldRole }, new { NewRole = request.NewRole }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new CommitteeMemberDto(
            member.Id, member.UserId, member.User.FullNameAr, member.User.FullNameEn,
            member.User.Email, member.Role, member.IsActive, member.JoinedAt, member.LeftAt
        );

        return ApiResponse<CommitteeMemberDto>.Success(dto, "Member role updated successfully.");
    }
}
