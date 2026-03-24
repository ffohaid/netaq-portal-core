using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Proposals.Commands;

// ===== DTOs =====
public class ProposalDto
{
    public Guid Id { get; set; }
    public Guid TenderId { get; set; }
    public string VendorNameAr { get; set; } = string.Empty;
    public string VendorNameEn { get; set; } = string.Empty;
    public string VendorReferenceNumber { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public bool? PassedComplianceCheck { get; set; }
    public string? ComplianceFailureReason { get; set; }
    public decimal? TechnicalScore { get; set; }
    public bool? PassedTechnicalEvaluation { get; set; }
    public decimal? FinancialScore { get; set; }
    public decimal? FinalScore { get; set; }
    public int? FinalRank { get; set; }
    public string? AiSummaryAr { get; set; }
    public string? AiSummaryEn { get; set; }
    public string? Notes { get; set; }
    public List<ProposalFileDto> Files { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ProposalFileDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsTextExtracted { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===== Create Proposal Command =====
public record CreateProposalCommand(
    Guid TenderId,
    string VendorNameAr,
    string VendorNameEn,
    string VendorReferenceNumber,
    decimal TotalValue,
    DateTime? ReceivedDate,
    string? Notes
) : IRequest<ApiResponse<ProposalDto>>;

public class CreateProposalCommandHandler : IRequestHandler<CreateProposalCommand, ApiResponse<ProposalDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateProposalCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<ProposalDto>> Handle(CreateProposalCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<ProposalDto>.Fail("Tender not found.");

        if (tender.Status != TenderStatus.Approved && tender.Status != TenderStatus.EvaluationInProgress)
            return ApiResponse<ProposalDto>.Fail("Proposals can only be uploaded when tender is in Approved or EvaluationInProgress status.");

        if (tender.IsReceiptClosed)
            return ApiResponse<ProposalDto>.Fail("Proposal receipt has been closed for this tender.");

        // Check for duplicate vendor reference
        var exists = await _context.Proposals
            .AnyAsync(p => p.TenderId == request.TenderId && p.VendorReferenceNumber == request.VendorReferenceNumber, cancellationToken);
        if (exists)
            return ApiResponse<ProposalDto>.Fail($"A proposal with vendor reference '{request.VendorReferenceNumber}' already exists for this tender.");

        var proposal = new Proposal
        {
            OrganizationId = tender.OrganizationId,
            TenderId = request.TenderId,
            VendorNameAr = request.VendorNameAr,
            VendorNameEn = request.VendorNameEn,
            VendorReferenceNumber = request.VendorReferenceNumber,
            TotalValue = request.TotalValue,
            ReceivedDate = request.ReceivedDate ?? DateTime.UtcNow,
            Notes = request.Notes,
            Status = ProposalStatus.Received,
            CreatedBy = _currentUser.UserId
        };

        _context.Proposals.Add(proposal);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProposalDto>.Ok(MapToDto(proposal));
    }

    private static ProposalDto MapToDto(Proposal p) => new()
    {
        Id = p.Id,
        TenderId = p.TenderId,
        VendorNameAr = p.VendorNameAr,
        VendorNameEn = p.VendorNameEn,
        VendorReferenceNumber = p.VendorReferenceNumber,
        TotalValue = p.TotalValue,
        Status = p.Status.ToString(),
        ReceivedDate = p.ReceivedDate,
        PassedComplianceCheck = p.PassedComplianceCheck,
        ComplianceFailureReason = p.ComplianceFailureReason,
        TechnicalScore = p.TechnicalScore,
        PassedTechnicalEvaluation = p.PassedTechnicalEvaluation,
        FinancialScore = p.FinancialScore,
        FinalScore = p.FinalScore,
        FinalRank = p.FinalRank,
        AiSummaryAr = p.AiSummaryAr,
        AiSummaryEn = p.AiSummaryEn,
        Notes = p.Notes,
        CreatedAt = p.CreatedAt,
        Files = p.Files?.Select(f => new ProposalFileDto
        {
            Id = f.Id,
            OriginalFileName = f.OriginalFileName,
            ContentType = f.ContentType,
            FileSizeBytes = f.FileSizeBytes,
            Category = f.Category.ToString(),
            IsTextExtracted = f.IsTextExtracted,
            CreatedAt = f.CreatedAt
        }).ToList() ?? new()
    };
}

// ===== Close Proposal Receipt Command =====
public record CloseProposalReceiptCommand(Guid TenderId) : IRequest<ApiResponse<string>>;

public class CloseProposalReceiptCommandHandler : IRequestHandler<CloseProposalReceiptCommand, ApiResponse<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CloseProposalReceiptCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<string>> Handle(CloseProposalReceiptCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .Include(t => t.Proposals)
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<string>.Fail("Tender not found.");

        if (tender.IsReceiptClosed)
            return ApiResponse<string>.Fail("Proposal receipt is already closed.");

        if (!tender.Proposals.Any())
            return ApiResponse<string>.Fail("Cannot close receipt without any proposals uploaded.");

        tender.IsReceiptClosed = true;
        tender.ReceiptClosedAt = DateTime.UtcNow;
        tender.ReceiptClosedBy = _currentUser.UserId;
        tender.PreviousStatus = tender.Status;
        tender.Status = TenderStatus.EvaluationInProgress;
        tender.UpdatedAt = DateTime.UtcNow;
        tender.UpdatedBy = _currentUser.UserId;

        // Create default compliance checklist if none exists
        var hasChecklist = await _context.ComplianceChecklists
            .AnyAsync(c => c.TenderId == request.TenderId, cancellationToken);

        if (!hasChecklist)
        {
            var defaultItems = GetDefaultComplianceItems(request.TenderId);
            foreach (var item in defaultItems)
            {
                _context.ComplianceChecklists.Add(item);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<string>.Ok("Proposal receipt closed successfully. Evaluation phase has begun.");
    }

    private static List<ComplianceChecklist> GetDefaultComplianceItems(Guid tenderId)
    {
        return new List<ComplianceChecklist>
        {
            new() { TenderId = tenderId, NameAr = "الضمان الابتدائي", NameEn = "Preliminary Guarantee", OrderIndex = 1, IsMandatory = true, IsDefault = true },
            new() { TenderId = tenderId, NameAr = "شهادة الزكاة والدخل", NameEn = "Zakat & Income Certificate", OrderIndex = 2, IsMandatory = true, IsDefault = true },
            new() { TenderId = tenderId, NameAr = "شهادة التأمينات الاجتماعية", NameEn = "Social Insurance Certificate", OrderIndex = 3, IsMandatory = true, IsDefault = true },
            new() { TenderId = tenderId, NameAr = "السجل التجاري", NameEn = "Commercial Register", OrderIndex = 4, IsMandatory = true, IsDefault = true },
            new() { TenderId = tenderId, NameAr = "شهادة المحتوى المحلي", NameEn = "Local Content Certificate", OrderIndex = 5, IsMandatory = true, IsDefault = true },
            new() { TenderId = tenderId, NameAr = "تصنيف المقاول", NameEn = "Contractor Classification", OrderIndex = 6, IsMandatory = false, IsDefault = true }
        };
    }
}

// ===== Upload Proposal File Command =====
public record UploadProposalFileCommand(
    Guid ProposalId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string ObjectKey,
    string StoredFileName,
    string BucketName,
    string FileHash,
    ProposalFileCategory Category
) : IRequest<ApiResponse<ProposalFileDto>>;

public class UploadProposalFileCommandHandler : IRequestHandler<UploadProposalFileCommand, ApiResponse<ProposalFileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UploadProposalFileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<ProposalFileDto>> Handle(UploadProposalFileCommand request, CancellationToken cancellationToken)
    {
        var proposal = await _context.Proposals
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, cancellationToken);

        if (proposal == null)
            return ApiResponse<ProposalFileDto>.Fail("Proposal not found.");

        // Validate file size (max 100MB)
        if (request.FileSizeBytes > 100 * 1024 * 1024)
            return ApiResponse<ProposalFileDto>.Fail("File size exceeds the maximum limit of 100MB.");

        var file = new ProposalFile
        {
            ProposalId = request.ProposalId,
            OriginalFileName = request.FileName,
            StoredFileName = request.StoredFileName,
            BucketName = request.BucketName,
            ObjectKey = request.ObjectKey,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            FileHash = request.FileHash,
            Category = request.Category,
            CreatedBy = _currentUser.UserId
        };

        _context.ProposalFiles.Add(file);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProposalFileDto>.Ok(new ProposalFileDto
        {
            Id = file.Id,
            OriginalFileName = file.OriginalFileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.FileSizeBytes,
            Category = file.Category.ToString(),
            IsTextExtracted = file.IsTextExtracted,
            CreatedAt = file.CreatedAt
        });
    }
}
