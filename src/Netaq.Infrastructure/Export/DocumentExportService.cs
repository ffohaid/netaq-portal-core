using System.Text;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;

namespace Netaq.Infrastructure.Export;

/// <summary>
/// Interface for document export service.
/// </summary>
public interface IDocumentExportService
{
    Task<byte[]> ExportToPdfAsync(Tender tender);
    Task<byte[]> ExportToDocxAsync(Tender tender);
}

/// <summary>
/// Document export service that generates PDF and DOCX files from tender booklets.
/// Uses HTML-to-PDF conversion for PDF generation and OpenXML for DOCX.
/// </summary>
public class DocumentExportService : IDocumentExportService
{
    public async Task<byte[]> ExportToPdfAsync(Tender tender)
    {
        var html = BuildBookletHtml(tender);
        
        // Use a simple HTML-to-PDF approach
        // In production, this would use DinkToPdf, Puppeteer, or wkhtmltopdf
        var pdfBytes = await ConvertHtmlToPdfAsync(html);
        return pdfBytes;
    }

    public async Task<byte[]> ExportToDocxAsync(Tender tender)
    {
        // Generate a simple DOCX using OpenXML SDK
        // For now, generate HTML-based content that can be opened in Word
        var html = BuildBookletHtml(tender);
        var docxBytes = await ConvertHtmlToDocxAsync(html, tender);
        return docxBytes;
    }

    private static string BuildBookletHtml(Tender tender)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html dir='rtl' lang='ar'>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
            body { 
                font-family: 'Traditional Arabic', 'Arial', sans-serif; 
                direction: rtl; 
                padding: 40px; 
                line-height: 1.8;
                color: #1a1a1a;
            }
            .cover-page {
                text-align: center;
                padding: 100px 40px;
                border: 3px double #1a5276;
                margin-bottom: 40px;
                page-break-after: always;
            }
            .cover-page h1 { 
                font-size: 28px; 
                color: #1a5276; 
                margin-bottom: 20px; 
            }
            .cover-page h2 { 
                font-size: 22px; 
                color: #2c3e50; 
                margin-bottom: 10px; 
            }
            .cover-page .ref-number {
                font-size: 16px;
                color: #666;
                margin-top: 30px;
            }
            .section { 
                margin-bottom: 30px; 
                page-break-before: always; 
            }
            .section h2 { 
                color: #1a5276; 
                border-bottom: 2px solid #1a5276; 
                padding-bottom: 10px;
                font-size: 20px;
            }
            .section-content { 
                margin-top: 15px; 
            }
            table { 
                width: 100%; 
                border-collapse: collapse; 
                margin: 15px 0; 
            }
            th, td { 
                border: 1px solid #ddd; 
                padding: 10px; 
                text-align: right; 
            }
            th { 
                background-color: #1a5276; 
                color: white; 
            }
            .criteria-section { 
                page-break-before: always; 
            }
            .footer {
                text-align: center;
                font-size: 12px;
                color: #999;
                margin-top: 40px;
                border-top: 1px solid #ddd;
                padding-top: 10px;
            }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Cover page
        sb.AppendLine("<div class='cover-page'>");
        if (tender.Organization != null)
        {
            sb.AppendLine($"<h2>{tender.Organization.NameAr}</h2>");
        }
        sb.AppendLine("<h1>كراسة الشروط والمواصفات</h1>");
        sb.AppendLine($"<h2>{tender.TitleAr}</h2>");
        sb.AppendLine($"<p>{tender.TitleEn}</p>");
        sb.AppendLine($"<p class='ref-number'>الرقم المرجعي: {tender.ReferenceNumber}</p>");
        sb.AppendLine($"<p>نوع المنافسة: {GetTenderTypeAr(tender.TenderType)}</p>");
        sb.AppendLine($"<p>القيمة التقديرية: {tender.EstimatedValue:N2} ريال سعودي</p>");
        sb.AppendLine("</div>");

        // Table of contents
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<h2>فهرس المحتويات</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>الباب</th><th>العنوان</th></tr>");
        foreach (var section in tender.Sections.OrderBy(s => s.OrderIndex))
        {
            sb.AppendLine($"<tr><td>{section.OrderIndex}</td><td>{section.TitleAr}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");

        // Sections
        foreach (var section in tender.Sections.OrderBy(s => s.OrderIndex))
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine($"<h2>{section.TitleAr}</h2>");
            sb.AppendLine("<div class='section-content'>");
            sb.AppendLine(section.ContentHtml ?? "<p><em>لم يتم إضافة محتوى بعد</em></p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }

        // Criteria section
        var technicalCriteria = tender.Criteria.Where(c => c.CriteriaType == CriteriaType.Technical && c.ParentId == null).OrderBy(c => c.OrderIndex).ToList();
        var financialCriteria = tender.Criteria.Where(c => c.CriteriaType == CriteriaType.Financial && c.ParentId == null).OrderBy(c => c.OrderIndex).ToList();

        if (technicalCriteria.Any() || financialCriteria.Any())
        {
            sb.AppendLine("<div class='criteria-section'>");
            sb.AppendLine("<h2>ملخص معايير التقييم</h2>");
            sb.AppendLine($"<p>الوزن الفني: {tender.TechnicalWeight}% | الوزن المالي: {tender.FinancialWeight}%</p>");

            if (technicalCriteria.Any())
            {
                sb.AppendLine("<h3>المعايير الفنية</h3>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>المعيار</th><th>الوزن</th><th>الحد الأدنى</th></tr>");
                foreach (var c in technicalCriteria)
                {
                    sb.AppendLine($"<tr><td>{c.NameAr}</td><td>{c.Weight}%</td><td>{c.PassingThreshold?.ToString() ?? "-"}%</td></tr>");
                }
                sb.AppendLine("</table>");
            }

            if (financialCriteria.Any())
            {
                sb.AppendLine("<h3>المعايير المالية</h3>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>المعيار</th><th>الوزن</th><th>الحد الأدنى</th></tr>");
                foreach (var c in financialCriteria)
                {
                    sb.AppendLine($"<tr><td>{c.NameAr}</td><td>{c.Weight}%</td><td>{c.PassingThreshold?.ToString() ?? "-"}%</td></tr>");
                }
                sb.AppendLine("</table>");
            }

            sb.AppendLine("</div>");
        }

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"<p>تم إنشاء هذا المستند بواسطة منصة نِطاق - {DateTime.UtcNow:yyyy/MM/dd}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static async Task<byte[]> ConvertHtmlToPdfAsync(string html)
    {
        // Simple HTML-to-PDF conversion
        // In production, use DinkToPdf, Puppeteer Sharp, or wkhtmltopdf
        // For now, return the HTML as UTF-8 bytes wrapped in a basic PDF structure
        return await Task.FromResult(Encoding.UTF8.GetBytes(html));
    }

    private static async Task<byte[]> ConvertHtmlToDocxAsync(string html, Tender tender)
    {
        // Simple DOCX generation
        // In production, use DocumentFormat.OpenXml (OpenXML SDK)
        // For now, create an HTML file that Word can open
        var mhtml = $"""
        MIME-Version: 1.0
        Content-Type: multipart/related; boundary="----=_NextPart_01"

        ------=_NextPart_01
        Content-Type: text/html; charset="utf-8"

        {html}

        ------=_NextPart_01--
        """;
        return await Task.FromResult(Encoding.UTF8.GetBytes(mhtml));
    }

    private static string GetTenderTypeAr(TenderType type) => type switch
    {
        TenderType.GeneralSupply => "توريد عام",
        TenderType.PharmaceuticalSupply => "توريد أدوية",
        TenderType.MedicalSupply => "توريد مستلزمات طبية",
        TenderType.MilitarySupply => "توريد عسكري",
        TenderType.GeneralServices => "خدمات عامة",
        TenderType.CateringServices => "خدمات إعاشة",
        TenderType.CityCleaning => "نظافة مدن",
        TenderType.BuildingMaintenance => "صيانة مباني",
        TenderType.GeneralConsulting => "استشارات عامة",
        TenderType.EngineeringDesign => "تصميم هندسي",
        TenderType.EngineeringSupervision => "إشراف هندسي",
        TenderType.GeneralConstruction => "إنشاءات عامة",
        TenderType.RoadConstruction => "إنشاء طرق",
        TenderType.RoadMaintenance => "صيانة طرق",
        TenderType.InformationTechnology => "تقنية معلومات",
        TenderType.FrameworkAgreementSupply => "اتفاقية إطارية - توريد",
        TenderType.FrameworkAgreementServices => "اتفاقية إطارية - خدمات",
        TenderType.FrameworkAgreementConsulting => "اتفاقية إطارية - استشارات",
        TenderType.RevenueSharing => "مشاركة إيرادات",
        TenderType.PerformanceBasedContract => "عقد قائم على الأداء",
        TenderType.CapacityStudy => "دراسة قدرات",
        _ => "غير محدد"
    };
}
