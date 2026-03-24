using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Export;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IDocumentExportService _exportService;

    public ExportController(IApplicationDbContext context, IDocumentExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    /// <summary>
    /// Export tender booklet as PDF.
    /// </summary>
    [HttpGet("tenders/{tenderId:guid}/pdf")]
    public async Task<IActionResult> ExportPdf(Guid tenderId)
    {
        var tender = await _context.Tenders
            .Include(t => t.Sections.OrderBy(s => s.OrderIndex))
            .Include(t => t.Criteria)
            .Include(t => t.Organization)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenderId);

        if (tender == null)
            return NotFound("Tender not found.");

        var pdfBytes = await _exportService.ExportToPdfAsync(tender);
        var fileName = $"Booklet_{tender.ReferenceNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Export tender booklet as Word document.
    /// </summary>
    [HttpGet("tenders/{tenderId:guid}/docx")]
    public async Task<IActionResult> ExportDocx(Guid tenderId)
    {
        var tender = await _context.Tenders
            .Include(t => t.Sections.OrderBy(s => s.OrderIndex))
            .Include(t => t.Criteria)
            .Include(t => t.Organization)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenderId);

        if (tender == null)
            return NotFound("Tender not found.");

        var docxBytes = await _exportService.ExportToDocxAsync(tender);
        var fileName = $"Booklet_{tender.ReferenceNumber}_{DateTime.UtcNow:yyyyMMdd}.docx";
        return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }
}
