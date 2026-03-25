using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Application.Inquiries.Commands;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Inquiries.Queries;

// ==================== Get Inquiries List ====================
public record GetInquiriesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? TenderId = null,
    InquiryStatus? Status = null,
    InquiryCategory? Category = null,
    string? Search = null
) : IRequest<ApiResponse<PaginatedResponse<InquiryDto>>>;

public class GetInquiriesQueryHandler : IRequestHandler<GetInquiriesQuery, ApiResponse<PaginatedResponse<InquiryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInquiriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedResponse<InquiryDto>>> Handle(GetInquiriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.AssignedToUser)
            .AsQueryable();

        if (request.TenderId.HasValue)
            query = query.Where(i => i.TenderId == request.TenderId.Value);

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.Category.HasValue)
            query = query.Where(i => i.Category == request.Category.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(i =>
                i.SubjectAr.ToLower().Contains(search) ||
                i.SubjectEn.ToLower().Contains(search) ||
                i.QuestionAr.ToLower().Contains(search) ||
                i.QuestionEn.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InquiryDto(
                i.Id, i.TenderId,
                i.Tender.TitleAr, i.Tender.TitleEn,
                i.SubjectAr, i.SubjectEn,
                i.QuestionAr, i.QuestionEn,
                i.ResponseAr, i.ResponseEn,
                i.Status, i.Priority, i.Category,
                i.SubmittedByUserId,
                i.SubmittedByUser.FullNameAr, i.SubmittedByUser.FullNameEn,
                i.AssignedToUserId,
                i.AssignedToUser != null ? i.AssignedToUser.FullNameAr : null,
                i.AssignedToUser != null ? i.AssignedToUser.FullNameEn : null,
                i.TenderSectionId,
                i.DueDate, i.RespondedAt, i.ClosedAt,
                i.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var response = new PaginatedResponse<InquiryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return ApiResponse<PaginatedResponse<InquiryDto>>.Success(response);
    }
}

// ==================== Get Inquiry Detail ====================
public record GetInquiryDetailQuery(Guid InquiryId) : IRequest<ApiResponse<InquiryDto>>;

public class GetInquiryDetailQueryHandler : IRequestHandler<GetInquiryDetailQuery, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInquiryDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(GetInquiryDetailQuery request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.AssignedToUser)
            .FirstOrDefaultAsync(i => i.Id == request.InquiryId, cancellationToken);

        if (inquiry == null)
            return ApiResponse<InquiryDto>.Failure("Inquiry not found.");

        var dto = CreateInquiryCommandHandler.MapToDto(inquiry, inquiry.Tender, inquiry.SubmittedByUser, inquiry.AssignedToUser);
        return ApiResponse<InquiryDto>.Success(dto);
    }
}

// ==================== Get Inquiry Statistics ====================
public record InquiryStatsDto(
    int TotalInquiries,
    int SubmittedCount,
    int UnderReviewCount,
    int RespondedCount,
    int ClosedCount,
    int OverdueCount
);

public record GetInquiryStatsQuery(Guid? TenderId = null) : IRequest<ApiResponse<InquiryStatsDto>>;

public class GetInquiryStatsQueryHandler : IRequestHandler<GetInquiryStatsQuery, ApiResponse<InquiryStatsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInquiryStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<InquiryStatsDto>> Handle(GetInquiryStatsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Inquiries.AsQueryable();

        if (request.TenderId.HasValue)
            query = query.Where(i => i.TenderId == request.TenderId.Value);

        var now = DateTime.UtcNow;
        var stats = new InquiryStatsDto(
            TotalInquiries: await query.CountAsync(cancellationToken),
            SubmittedCount: await query.CountAsync(i => i.Status == InquiryStatus.Submitted, cancellationToken),
            UnderReviewCount: await query.CountAsync(i => i.Status == InquiryStatus.UnderReview, cancellationToken),
            RespondedCount: await query.CountAsync(i => i.Status == InquiryStatus.Responded, cancellationToken),
            ClosedCount: await query.CountAsync(i => i.Status == InquiryStatus.Closed, cancellationToken),
            OverdueCount: await query.CountAsync(i => i.DueDate < now && i.Status != InquiryStatus.Closed && i.Status != InquiryStatus.Responded, cancellationToken)
        );

        return ApiResponse<InquiryStatsDto>.Success(stats);
    }
}

// ==================== Export Inquiries ====================
public record ExportInquiriesQuery(Guid? TenderId = null, InquiryStatus? Status = null) : IRequest<ApiResponse<byte[]>>;

public class ExportInquiriesQueryHandler : IRequestHandler<ExportInquiriesQuery, ApiResponse<byte[]>>
{
    private readonly IApplicationDbContext _context;

    public ExportInquiriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<byte[]>> Handle(ExportInquiriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.AssignedToUser)
            .AsQueryable();

        if (request.TenderId.HasValue)
            query = query.Where(i => i.TenderId == request.TenderId.Value);

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        var inquiries = await query.OrderByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);

        // Build CSV
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID,Subject (AR),Subject (EN),Status,Priority,Category,Tender,Submitted By,Assigned To,Due Date,Responded At,Created At");

        foreach (var i in inquiries)
        {
            var line = string.Join(",",
                i.Id,
                EscapeCsv(i.SubjectAr),
                EscapeCsv(i.SubjectEn),
                i.Status.ToString(),
                i.Priority.ToString(),
                i.Category.ToString(),
                EscapeCsv(i.Tender?.TitleEn ?? ""),
                EscapeCsv(i.SubmittedByUser?.FullNameEn ?? ""),
                EscapeCsv(i.AssignedToUser?.FullNameEn ?? ""),
                i.DueDate?.ToString("yyyy-MM-dd") ?? "",
                i.RespondedAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                i.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            );
            sb.AppendLine(line);
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return ApiResponse<byte[]>.Success(bytes);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
