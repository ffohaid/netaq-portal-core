using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Invitations.Commands;
using Netaq.Domain.Enums;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly IAuditTrailService _auditTrailService;
    private readonly ILogger<InvitationController> _logger;

    public InvitationController(
        IMediator mediator,
        ICurrentUserService currentUser,
        IEmailService emailService,
        IAuditTrailService auditTrailService,
        ILogger<InvitationController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _emailService = emailService;
        _auditTrailService = auditTrailService;
        _logger = logger;
    }

    /// <summary>
    /// Send an invitation to a new user (System Admin only).
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<Guid>>> SendInvitation([FromBody] SendInvitationRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var command = new SendInvitationCommand(
            _currentUser.OrganizationId.Value,
            request.Email,
            request.FullNameAr,
            request.FullNameEn,
            request.AssignedRole,
            _currentUser.UserId.Value);

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            // Log audit trail first (this should always succeed)
            await _auditTrailService.LogAsync(
                _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
                AuditActionCategory.UserManagement, "INVITATION_SENT",
                $"Invitation sent to {request.Email}",
                "Invitation", result.Data,
                ipAddress: _currentUser.IpAddress,
                userAgent: _currentUser.UserAgent);

            // Try to send invitation email - non-blocking on failure
            try
            {
                var dbContext = HttpContext.RequestServices
                    .GetRequiredService<Domain.Interfaces.IApplicationDbContext>();

                var org = await dbContext.Organizations.FindAsync(_currentUser.OrganizationId.Value);
                if (org != null)
                {
                    var invitation = await dbContext.Invitations.FindAsync(result.Data);
                    if (invitation != null)
                    {
                        await _emailService.SendInvitationAsync(request.Email, invitation.Token, org.NameEn);
                        _logger.LogInformation("Invitation email sent successfully to {Email}", request.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the email failure but don't fail the invitation
                _logger.LogWarning(ex, "Failed to send invitation email to {Email}. Invitation was saved successfully and can be resent later.", request.Email);
            }
        }

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Accept an invitation and create user account.
    /// </summary>
    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Guid>>> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        var command = new AcceptInvitationCommand(
            request.Token,
            request.FullNameAr,
            request.FullNameEn,
            request.Password,
            request.Phone,
            request.JobTitleAr,
            request.JobTitleEn);

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
