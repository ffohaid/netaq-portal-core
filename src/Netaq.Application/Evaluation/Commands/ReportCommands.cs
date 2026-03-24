using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Evaluation.Commands;

// ===== Report DTOs =====
public class EvaluationReportDto
{
    public Guid Id { get; set; }
    public Guid TenderId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? PdfObjectKey { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public string? AiAwardJustification { get; set; }
    public List<SignatureDto> Signatures { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SignatureDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Comments { get; set; }
}

// ===== Generate Evaluation Report Command =====
public record GenerateEvaluationReportCommand(
    Guid TenderId,
    EvaluationReportType ReportType
) : IRequest<ApiResponse<EvaluationReportDto>>;

public class GenerateEvaluationReportCommandHandler : IRequestHandler<GenerateEvaluationReportCommand, ApiResponse<EvaluationReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GenerateEvaluationReportCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<EvaluationReportDto>> Handle(GenerateEvaluationReportCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .Include(t => t.Proposals)
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<EvaluationReportDto>.Fail("Tender not found.");

        // Generate reference number
        var reportCount = await _context.EvaluationReports
            .CountAsync(r => r.TenderId == request.TenderId, cancellationToken);
        var refNumber = $"RPT-{tender.ReferenceNumber}-{request.ReportType.ToString()[..3].ToUpper()}-{reportCount + 1:D3}";

        string titleAr, titleEn, contentHtml;

        switch (request.ReportType)
        {
            case EvaluationReportType.ComplianceInspection:
                (titleAr, titleEn, contentHtml) = await GenerateComplianceReport(tender, cancellationToken);
                break;
            case EvaluationReportType.TechnicalEvaluation:
                (titleAr, titleEn, contentHtml) = await GenerateTechnicalReport(tender, cancellationToken);
                break;
            case EvaluationReportType.FinalEvaluation:
                (titleAr, titleEn, contentHtml) = await GenerateFinalReport(tender, cancellationToken);
                break;
            default:
                return ApiResponse<EvaluationReportDto>.Fail("Invalid report type.");
        }

        var report = new EvaluationReport
        {
            OrganizationId = tender.OrganizationId,
            TenderId = request.TenderId,
            ReportType = request.ReportType,
            TitleAr = titleAr,
            TitleEn = titleEn,
            ReferenceNumber = refNumber,
            ContentHtml = contentHtml,
            Status = EvaluationReportStatus.Draft,
            CreatedBy = _currentUser.UserId
        };

        _context.EvaluationReports.Add(report);

        // Add committee members as signature slots
        var committee = await _context.Committees
            .Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.TenderId == request.TenderId, cancellationToken);

        if (committee != null)
        {
            foreach (var member in committee.Members)
            {
                var signature = new ReportSignature
                {
                    EvaluationReportId = report.Id,
                    SignedByUserId = member.UserId,
                    SignerRole = member.Role,
                    IsSigned = false,
                    CreatedBy = _currentUser.UserId
                };
                _context.ReportSignatures.Add(signature);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<EvaluationReportDto>.Ok(new EvaluationReportDto
        {
            Id = report.Id,
            TenderId = report.TenderId,
            ReportType = report.ReportType.ToString(),
            TitleAr = report.TitleAr,
            TitleEn = report.TitleEn,
            ReferenceNumber = report.ReferenceNumber,
            Status = report.Status.ToString(),
            ContentHtml = report.ContentHtml,
            AiAwardJustification = report.AiAwardJustification,
            CreatedAt = report.CreatedAt,
            Signatures = new()
        });
    }

    private async Task<(string titleAr, string titleEn, string contentHtml)> GenerateComplianceReport(
        Tender tender, CancellationToken cancellationToken)
    {
        var proposals = await _context.Proposals
            .Include(p => p.ComplianceResults).ThenInclude(r => r.ChecklistItem)
            .Where(p => p.TenderId == tender.Id)
            .ToListAsync(cancellationToken);

        var html = $@"
<div dir='rtl' style='font-family: Arial, sans-serif; padding: 20px;'>
    <h1 style='text-align: center; color: #1a365d;'>محضر الفحص النظامي</h1>
    <h2 style='text-align: center; color: #2d3748;'>{tender.TitleAr}</h2>
    <p style='text-align: center;'>الرقم المرجعي: {tender.ReferenceNumber}</p>
    <hr/>
    <h3>ملخص نتائج الفحص</h3>
    <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
        <thead>
            <tr style='background-color: #edf2f7;'>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>المتنافس</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>الرقم المرجعي</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>النتيجة</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>الملاحظات</th>
            </tr>
        </thead>
        <tbody>";

        foreach (var p in proposals)
        {
            var result = p.PassedComplianceCheck == true ? "اجتاز" : "لم يجتز";
            var color = p.PassedComplianceCheck == true ? "#38a169" : "#e53e3e";
            html += $@"
            <tr>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.VendorNameAr}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.VendorReferenceNumber}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px; color: {color}; font-weight: bold;'>{result}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.ComplianceFailureReason ?? "-"}</td>
            </tr>";
        }

        html += @"
        </tbody>
    </table>
    <div style='margin-top: 30px;'>
        <h3>التوقيعات</h3>
        <p>___________________</p>
    </div>
</div>";

        return ("محضر الفحص النظامي", "Compliance Inspection Report", html);
    }

    private async Task<(string titleAr, string titleEn, string contentHtml)> GenerateTechnicalReport(
        Tender tender, CancellationToken cancellationToken)
    {
        var proposals = await _context.Proposals
            .Where(p => p.TenderId == tender.Id && p.PassedComplianceCheck == true)
            .ToListAsync(cancellationToken);

        var html = $@"
<div dir='rtl' style='font-family: Arial, sans-serif; padding: 20px;'>
    <h1 style='text-align: center; color: #1a365d;'>محضر التقييم الفني</h1>
    <h2 style='text-align: center; color: #2d3748;'>{tender.TitleAr}</h2>
    <p style='text-align: center;'>الرقم المرجعي: {tender.ReferenceNumber}</p>
    <p style='text-align: center;'>الوزن الفني: {tender.TechnicalWeight}% | الوزن المالي: {tender.FinancialWeight}%</p>
    <hr/>
    <h3>نتائج التقييم الفني</h3>
    <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
        <thead>
            <tr style='background-color: #edf2f7;'>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>المتنافس</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>الدرجة الفنية</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>النتيجة</th>
            </tr>
        </thead>
        <tbody>";

        foreach (var p in proposals.OrderByDescending(p => p.TechnicalScore))
        {
            var result = p.PassedTechnicalEvaluation == true ? "مجتاز" : "غير مجتاز";
            var color = p.PassedTechnicalEvaluation == true ? "#38a169" : "#e53e3e";
            html += $@"
            <tr>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.VendorNameAr}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.TechnicalScore:F2}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px; color: {color}; font-weight: bold;'>{result}</td>
            </tr>";
        }

        html += @"
        </tbody>
    </table>
    <div style='margin-top: 30px;'>
        <h3>التوقيعات</h3>
        <p>___________________</p>
    </div>
</div>";

        return ("محضر التقييم الفني", "Technical Evaluation Report", html);
    }

    private async Task<(string titleAr, string titleEn, string contentHtml)> GenerateFinalReport(
        Tender tender, CancellationToken cancellationToken)
    {
        var proposals = await _context.Proposals
            .Where(p => p.TenderId == tender.Id && p.FinalRank != null)
            .OrderBy(p => p.FinalRank)
            .ToListAsync(cancellationToken);

        var html = $@"
<div dir='rtl' style='font-family: Arial, sans-serif; padding: 20px;'>
    <h1 style='text-align: center; color: #1a365d;'>محضر التقييم النهائي</h1>
    <h2 style='text-align: center; color: #2d3748;'>{tender.TitleAr}</h2>
    <p style='text-align: center;'>الرقم المرجعي: {tender.ReferenceNumber}</p>
    <p style='text-align: center;'>الوزن الفني: {tender.TechnicalWeight}% | الوزن المالي: {tender.FinancialWeight}%</p>
    <hr/>
    <h3>الترتيب النهائي للمتنافسين</h3>
    <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
        <thead>
            <tr style='background-color: #edf2f7;'>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>الترتيب</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>المتنافس</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>الدرجة الفنية</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>الدرجة المالية</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>الدرجة النهائية</th>
                <th style='border: 1px solid #cbd5e0; padding: 8px;'>التوصية</th>
            </tr>
        </thead>
        <tbody>";

        foreach (var p in proposals)
        {
            var recommendation = p.Status == ProposalStatus.Recommended ? "موصى بالترسية" : "-";
            var bgColor = p.Status == ProposalStatus.Recommended ? "background-color: #f0fff4;" : "";
            html += $@"
            <tr style='{bgColor}'>
                <td style='border: 1px solid #cbd5e0; padding: 8px; text-align: center; font-weight: bold;'>{p.FinalRank}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.VendorNameAr}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.TechnicalScore:F2}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px;'>{p.FinancialScore:F2}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px; font-weight: bold;'>{p.FinalScore:F2}</td>
                <td style='border: 1px solid #cbd5e0; padding: 8px; color: #38a169;'>{recommendation}</td>
            </tr>";
        }

        html += @"
        </tbody>
    </table>
    <div style='margin-top: 30px;'>
        <h3>التوقيعات</h3>
        <p>___________________</p>
    </div>
</div>";

        return ("محضر التقييم النهائي", "Final Evaluation Report", html);
    }
}

// ===== Sign Report Command =====
public record SignReportCommand(
    Guid ReportId,
    string? Comments
) : IRequest<ApiResponse<SignatureDto>>;

public class SignReportCommandHandler : IRequestHandler<SignReportCommand, ApiResponse<SignatureDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SignReportCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<SignatureDto>> Handle(SignReportCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var signature = await _context.ReportSignatures
            .Include(s => s.SignedByUser)
            .FirstOrDefaultAsync(s => s.EvaluationReportId == request.ReportId && s.SignedByUserId == userId, cancellationToken);

        if (signature == null)
            return ApiResponse<SignatureDto>.Fail("You are not authorized to sign this report.");

        if (signature.IsSigned)
            return ApiResponse<SignatureDto>.Fail("You have already signed this report.");

        // Generate content hash for integrity
        var report = await _context.EvaluationReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report == null)
            return ApiResponse<SignatureDto>.Fail("Report not found.");

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(report.ContentHtml));
        var contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        signature.IsSigned = true;
        signature.SignedAt = DateTime.UtcNow;
        signature.Comments = request.Comments;
        signature.ContentHash = contentHash;
        signature.UpdatedAt = DateTime.UtcNow;
        signature.UpdatedBy = _currentUser.UserId;

        // Check if all signatures are complete
        var allSignatures = await _context.ReportSignatures
            .Where(s => s.EvaluationReportId == request.ReportId)
            .ToListAsync(cancellationToken);

        if (allSignatures.All(s => s.IsSigned))
        {
            report.Status = EvaluationReportStatus.Signed;
            report.FinalizedAt = DateTime.UtcNow;
            report.FinalizedBy = _currentUser.UserId;
        }
        else
        {
            report.Status = EvaluationReportStatus.PendingSignatures;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SignatureDto>.Ok(new SignatureDto
        {
            Id = signature.Id,
            UserId = signature.SignedByUserId,
            UserName = signature.SignedByUser?.FullNameAr ?? "Unknown",
            Role = signature.SignerRole.ToString(),
            IsSigned = true,
            SignedAt = signature.SignedAt,
            Comments = signature.Comments
        });
    }
}
