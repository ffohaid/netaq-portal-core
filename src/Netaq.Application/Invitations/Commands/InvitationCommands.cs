using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Invitations.Commands;

// --- DTOs ---
public record SendInvitationRequest(
    string Email,
    string? FullNameAr,
    string? FullNameEn,
    OrganizationRole AssignedRole);

public record AcceptInvitationRequest(
    string Token,
    string FullNameAr,
    string FullNameEn,
    string Password,
    string? Phone,
    string? JobTitleAr,
    string? JobTitleEn);

// --- Send Invitation Command ---
public record SendInvitationCommand(
    Guid OrganizationId,
    string Email,
    string? FullNameAr,
    string? FullNameEn,
    OrganizationRole AssignedRole,
    Guid SentByUserId) : IRequest<ApiResponse<Guid>>;

public class SendInvitationCommandValidator : AbstractValidator<SendInvitationCommand>
{
    public SendInvitationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AssignedRole).IsInEnum();
    }
}

public class SendInvitationCommandHandler : IRequestHandler<SendInvitationCommand, ApiResponse<Guid>>
{
    private readonly IApplicationDbContext _context;

    public SendInvitationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<Guid>> Handle(SendInvitationCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists in this organization
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
            return ApiResponse<Guid>.Failure("A user with this email already exists in the organization.");

        // Check for pending invitation
        var existingInvitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Email == request.Email && i.Status == InvitationStatus.Pending, cancellationToken);

        if (existingInvitation != null)
            return ApiResponse<Guid>.Failure("A pending invitation already exists for this email.");

        // Generate encrypted invitation token
        var token = GenerateInvitationToken();

        var invitation = new Invitation
        {
            OrganizationId = request.OrganizationId,
            Email = request.Email,
            FullNameAr = request.FullNameAr,
            FullNameEn = request.FullNameEn,
            AssignedRole = request.AssignedRole,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Status = InvitationStatus.Pending,
            CreatedBy = request.SentByUserId
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.Success(invitation.Id);
    }

    private static string GenerateInvitationToken()
    {
        var bytes = new byte[48];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

// --- Accept Invitation Command ---
public record AcceptInvitationCommand(
    string Token,
    string FullNameAr,
    string FullNameEn,
    string Password,
    string? Phone,
    string? JobTitleAr,
    string? JobTitleEn) : IRequest<ApiResponse<Guid>>;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FullNameEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, ApiResponse<Guid>>
{
    private readonly IApplicationDbContext _context;

    public AcceptInvitationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<Guid>> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _context.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Token == request.Token && i.Status == InvitationStatus.Pending && !i.IsDeleted, cancellationToken);

        if (invitation == null)
            return ApiResponse<Guid>.Failure("Invalid or expired invitation.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _context.SaveChangesAsync(cancellationToken);
            return ApiResponse<Guid>.Failure("Invitation has expired.");
        }

        // Create user account
        var (passwordHash, passwordSalt) = HashPassword(request.Password);

        var user = new User
        {
            OrganizationId = invitation.OrganizationId,
            FullNameAr = request.FullNameAr,
            FullNameEn = request.FullNameEn,
            Email = invitation.Email,
            Phone = request.Phone,
            JobTitleAr = request.JobTitleAr,
            JobTitleEn = request.JobTitleEn,
            Role = invitation.AssignedRole,
            Status = UserStatus.Active,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Locale = "ar"
        };

        _context.Users.Add(user);

        // Update invitation
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedByUserId = user.Id;
        invitation.AcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.Success(user.Id);
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        using var hmac = new HMACSHA512(saltBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hash = Convert.ToBase64String(hashBytes);

        return (hash, salt);
    }
}
