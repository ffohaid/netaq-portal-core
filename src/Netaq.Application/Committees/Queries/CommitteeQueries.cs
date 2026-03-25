using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Committees.Commands;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Committees.Queries;

// ==================== Get Committees List ====================
public record GetCommitteesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    CommitteeType? Type = null,
    bool? IsActive = null,
    string? Search = null
) : IRequest<ApiResponse<PaginatedResponse<CommitteeDto>>>;

public class GetCommitteesQueryHandler : IRequestHandler<GetCommitteesQuery, ApiResponse<PaginatedResponse<CommitteeDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCommitteesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedResponse<CommitteeDto>>> Handle(GetCommitteesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Committees
            .Include(c => c.Members.Where(m => m.IsActive))
            .Include(c => c.Tender)
            .AsQueryable();

        if (request.Type.HasValue)
            query = query.Where(c => c.Type == request.Type.Value);

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.NameAr.ToLower().Contains(search) ||
                c.NameEn.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CommitteeDto(
                c.Id, c.NameAr, c.NameEn, c.Type,
                c.PurposeAr, c.PurposeEn, c.IsActive,
                c.TenderId,
                c.Tender != null ? c.Tender.TitleAr : null,
                c.Tender != null ? c.Tender.TitleEn : null,
                c.FormedAt, c.DissolvedAt, c.CreatedAt,
                c.Members.Count(m => m.IsActive),
                null
            ))
            .ToListAsync(cancellationToken);

        var response = new PaginatedResponse<CommitteeDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return ApiResponse<PaginatedResponse<CommitteeDto>>.Success(response);
    }
}

// ==================== Get Committee Detail ====================
public record GetCommitteeDetailQuery(Guid CommitteeId) : IRequest<ApiResponse<CommitteeDto>>;

public class GetCommitteeDetailQueryHandler : IRequestHandler<GetCommitteeDetailQuery, ApiResponse<CommitteeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCommitteeDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<CommitteeDto>> Handle(GetCommitteeDetailQuery request, CancellationToken cancellationToken)
    {
        var committee = await _context.Committees
            .Include(c => c.Members.Where(m => m.IsActive))
                .ThenInclude(m => m.User)
            .Include(c => c.Tender)
            .FirstOrDefaultAsync(c => c.Id == request.CommitteeId, cancellationToken);

        if (committee == null)
            return ApiResponse<CommitteeDto>.Failure("Committee not found.");

        var members = committee.Members
            .Where(m => m.IsActive)
            .Select(m => new CommitteeMemberDto(
                m.Id, m.UserId, m.User.FullNameAr, m.User.FullNameEn,
                m.User.Email, m.Role, m.IsActive, m.JoinedAt, m.LeftAt
            ))
            .OrderBy(m => m.Role)
            .ToList();

        var dto = new CommitteeDto(
            committee.Id, committee.NameAr, committee.NameEn, committee.Type,
            committee.PurposeAr, committee.PurposeEn, committee.IsActive,
            committee.TenderId,
            committee.Tender?.TitleAr,
            committee.Tender?.TitleEn,
            committee.FormedAt, committee.DissolvedAt, committee.CreatedAt,
            members.Count,
            members
        );

        return ApiResponse<CommitteeDto>.Success(dto);
    }
}
