using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;

namespace Netaq.Infrastructure.Persistence;

/// <summary>
/// Seeds initial system data: default organization, system admin account,
/// and global booklet templates (21 templates across 7 categories).
/// Only runs on first startup when database is empty.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Only seed if no organizations exist
        if (await context.Organizations.IgnoreQueryFilters().AnyAsync())
        {
            // Still seed templates if they don't exist
            await SeedTemplatesIfNeeded(context);
            return;
        }

        var orgId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        // Create default organization
        var organization = new Organization
        {
            Id = orgId,
            NameAr = "الجهة الحكومية الافتراضية",
            NameEn = "Default Government Entity",
            DescriptionAr = "الجهة الحكومية الافتراضية لمنصة نِطاق",
            DescriptionEn = "Default government entity for NETAQ Portal",
            Email = "admin@netaq.pro",
            ActiveAuthProvider = AuthProviderType.CustomAuth,
            IsOtpEnabled = true,
            ShowPlatformLogo = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Organizations.Add(organization);

        // Create system admin user
        var (passwordHash, passwordSalt) = HashPassword("NetaqAdmin@2026!");
        
        var admin = new User
        {
            Id = adminId,
            OrganizationId = orgId,
            FullNameAr = "مدير النظام",
            FullNameEn = "System Administrator",
            Email = "admin@netaq.pro",
            Role = OrganizationRole.SystemAdmin,
            Status = UserStatus.Active,
            Locale = "ar",
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);

        // Create default permission matrix for system admin (full access)
        foreach (TenderPhase phase in Enum.GetValues<TenderPhase>())
        {
            context.PermissionMatrices.Add(new PermissionMatrix
            {
                OrganizationId = orgId,
                UserId = adminId,
                TenderPhase = phase,
                UserRole = OrganizationRole.SystemAdmin,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                CanReject = true,
                CanDelegate = true,
                CanExport = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        // Seed global booklet templates
        await SeedTemplatesIfNeeded(context);
    }

    private static async Task SeedTemplatesIfNeeded(ApplicationDbContext context)
    {
        if (await context.BookletTemplates.IgnoreQueryFilters().AnyAsync())
            return;

        var templates = GetGlobalTemplates();
        foreach (var template in templates)
        {
            context.BookletTemplates.Add(template);
            foreach (var section in template.Sections)
            {
                context.BookletTemplateSections.Add(section);
            }
        }

        await context.SaveChangesAsync();
    }

    private static List<BookletTemplate> GetGlobalTemplates()
    {
        var templates = new List<BookletTemplate>();

        // ============ Category 1: Supply (توريد) ============
        templates.Add(CreateTemplate("كراسة توريد عام", "General Supply Booklet",
            TemplateCategory.Supply, TenderType.GeneralSupply,
            "نموذج كراسة شروط ومواصفات لمنافسات التوريد العام", "Standard booklet template for general supply tenders"));

        templates.Add(CreateTemplate("كراسة توريد أدوية", "Pharmaceutical Supply Booklet",
            TemplateCategory.Supply, TenderType.PharmaceuticalSupply,
            "نموذج كراسة شروط ومواصفات لمنافسات توريد الأدوية", "Booklet template for pharmaceutical supply tenders"));

        templates.Add(CreateTemplate("كراسة توريد مستلزمات طبية", "Medical Supply Booklet",
            TemplateCategory.Supply, TenderType.MedicalSupply,
            "نموذج كراسة شروط ومواصفات لمنافسات توريد المستلزمات الطبية", "Booklet template for medical supply tenders"));

        // ============ Category 2: Services (خدمات) ============
        templates.Add(CreateTemplate("كراسة خدمات عامة", "General Services Booklet",
            TemplateCategory.Services, TenderType.GeneralServices,
            "نموذج كراسة شروط ومواصفات لمنافسات الخدمات العامة", "Booklet template for general services tenders"));

        templates.Add(CreateTemplate("كراسة خدمات إعاشة", "Catering Services Booklet",
            TemplateCategory.Services, TenderType.CateringServices,
            "نموذج كراسة شروط ومواصفات لمنافسات خدمات الإعاشة", "Booklet template for catering services tenders"));

        templates.Add(CreateTemplate("كراسة نظافة مدن", "City Cleaning Booklet",
            TemplateCategory.Services, TenderType.CityCleaning,
            "نموذج كراسة شروط ومواصفات لمنافسات نظافة المدن", "Booklet template for city cleaning tenders"));

        // ============ Category 3: Consulting (استشارات) ============
        templates.Add(CreateTemplate("كراسة استشارات عامة", "General Consulting Booklet",
            TemplateCategory.Consulting, TenderType.GeneralConsulting,
            "نموذج كراسة شروط ومواصفات لمنافسات الاستشارات العامة", "Booklet template for general consulting tenders"));

        // ============ Category 4: Engineering (هندسة) ============
        templates.Add(CreateTemplate("كراسة تصميم هندسي", "Engineering Design Booklet",
            TemplateCategory.Engineering, TenderType.EngineeringDesign,
            "نموذج كراسة شروط ومواصفات لمنافسات التصميم الهندسي", "Booklet template for engineering design tenders"));

        templates.Add(CreateTemplate("كراسة إشراف هندسي", "Engineering Supervision Booklet",
            TemplateCategory.Engineering, TenderType.EngineeringSupervision,
            "نموذج كراسة شروط ومواصفات لمنافسات الإشراف الهندسي", "Booklet template for engineering supervision tenders"));

        // ============ Category 5: IT (تقنية معلومات) ============
        templates.Add(CreateTemplate("كراسة تقنية معلومات", "Information Technology Booklet",
            TemplateCategory.InformationTechnology, TenderType.InformationTechnology,
            "نموذج كراسة شروط ومواصفات لمنافسات تقنية المعلومات", "Booklet template for IT tenders"));

        // ============ Category 6: Construction (إنشاءات) ============
        templates.Add(CreateTemplate("كراسة إنشاءات عامة", "General Construction Booklet",
            TemplateCategory.Construction, TenderType.GeneralConstruction,
            "نموذج كراسة شروط ومواصفات لمنافسات الإنشاءات العامة", "Booklet template for general construction tenders"));

        templates.Add(CreateTemplate("كراسة إنشاء طرق", "Road Construction Booklet",
            TemplateCategory.Construction, TenderType.RoadConstruction,
            "نموذج كراسة شروط ومواصفات لمنافسات إنشاء الطرق", "Booklet template for road construction tenders"));

        templates.Add(CreateTemplate("كراسة صيانة طرق", "Road Maintenance Booklet",
            TemplateCategory.Construction, TenderType.RoadMaintenance,
            "نموذج كراسة شروط ومواصفات لمنافسات صيانة الطرق", "Booklet template for road maintenance tenders"));

        templates.Add(CreateTemplate("كراسة صيانة مباني", "Building Maintenance Booklet",
            TemplateCategory.Construction, TenderType.BuildingMaintenance,
            "نموذج كراسة شروط ومواصفات لمنافسات صيانة المباني", "Booklet template for building maintenance tenders"));

        // ============ Category 7: Special Models (نماذج خاصة) ============
        templates.Add(CreateTemplate("كراسة اتفاقية إطارية - توريد", "Framework Agreement - Supply Booklet",
            TemplateCategory.SpecialModels, TenderType.FrameworkAgreementSupply,
            "نموذج كراسة شروط ومواصفات للاتفاقيات الإطارية - توريد", "Booklet template for framework agreement supply"));

        templates.Add(CreateTemplate("كراسة اتفاقية إطارية - خدمات", "Framework Agreement - Services Booklet",
            TemplateCategory.SpecialModels, TenderType.FrameworkAgreementServices,
            "نموذج كراسة شروط ومواصفات للاتفاقيات الإطارية - خدمات", "Booklet template for framework agreement services"));

        templates.Add(CreateTemplate("كراسة اتفاقية إطارية - استشارات", "Framework Agreement - Consulting Booklet",
            TemplateCategory.SpecialModels, TenderType.FrameworkAgreementConsulting,
            "نموذج كراسة شروط ومواصفات للاتفاقيات الإطارية - استشارات", "Booklet template for framework agreement consulting"));

        templates.Add(CreateTemplate("كراسة مشاركة إيرادات", "Revenue Sharing Booklet",
            TemplateCategory.SpecialModels, TenderType.RevenueSharing,
            "نموذج كراسة شروط ومواصفات لمنافسات مشاركة الإيرادات", "Booklet template for revenue sharing tenders"));

        templates.Add(CreateTemplate("كراسة عقد قائم على الأداء", "Performance-Based Contract Booklet",
            TemplateCategory.SpecialModels, TenderType.PerformanceBasedContract,
            "نموذج كراسة شروط ومواصفات للعقود القائمة على الأداء", "Booklet template for performance-based contracts"));

        templates.Add(CreateTemplate("كراسة توريد عسكري", "Military Supply Booklet",
            TemplateCategory.SpecialModels, TenderType.MilitarySupply,
            "نموذج كراسة شروط ومواصفات لمنافسات التوريد العسكري", "Booklet template for military supply tenders"));

        templates.Add(CreateTemplate("كراسة دراسة قدرات", "Capacity Study Booklet",
            TemplateCategory.SpecialModels, TenderType.CapacityStudy,
            "نموذج كراسة شروط ومواصفات لدراسات القدرات", "Booklet template for capacity study tenders"));

        return templates;
    }

    private static BookletTemplate CreateTemplate(
        string nameAr, string nameEn,
        TemplateCategory category, TenderType tenderType,
        string descAr, string descEn)
    {
        var templateId = Guid.NewGuid();
        var template = new BookletTemplate
        {
            Id = templateId,
            OrganizationId = null, // Global template
            NameAr = nameAr,
            NameEn = nameEn,
            Category = category,
            ApplicableTenderType = tenderType,
            DescriptionAr = descAr,
            DescriptionEn = descEn,
            IsActive = true,
            Version = "1.0",
            CreatedAt = DateTime.UtcNow
        };

        // Create 8 standard sections for each template
        var sectionTypes = Enum.GetValues<BookletSectionType>();
        for (int i = 0; i < sectionTypes.Length; i++)
        {
            var sectionType = sectionTypes[i];
            template.Sections.Add(new BookletTemplateSection
            {
                Id = Guid.NewGuid(),
                BookletTemplateId = templateId,
                SectionType = sectionType,
                TitleAr = GetSectionTitleAr(sectionType),
                TitleEn = GetSectionTitleEn(sectionType),
                DefaultContentHtml = GetDefaultSectionContent(sectionType, tenderType),
                OrderIndex = i + 1,
                GuidanceNotesAr = GetGuidanceNotesAr(sectionType),
                GuidanceNotesEn = GetGuidanceNotesEn(sectionType),
                CreatedAt = DateTime.UtcNow
            });
        }

        return template;
    }

    private static string GetSectionTitleAr(BookletSectionType type) => type switch
    {
        BookletSectionType.GeneralTermsAndConditions => "الباب الأول: الشروط والأحكام العامة",
        BookletSectionType.TechnicalScopeAndSpecifications => "الباب الثاني: النطاق الفني والمواصفات",
        BookletSectionType.QualificationRequirements => "الباب الثالث: متطلبات التأهيل",
        BookletSectionType.EvaluationCriteria => "الباب الرابع: معايير التقييم",
        BookletSectionType.FinancialTerms => "الباب الخامس: الشروط المالية",
        BookletSectionType.ContractualTerms => "الباب السادس: الشروط التعاقدية",
        BookletSectionType.LocalContentRequirements => "الباب السابع: متطلبات المحتوى المحلي",
        BookletSectionType.AppendicesAndForms => "الباب الثامن: الملاحق والنماذج",
        _ => "باب غير محدد"
    };

    private static string GetSectionTitleEn(BookletSectionType type) => type switch
    {
        BookletSectionType.GeneralTermsAndConditions => "Chapter 1: General Terms and Conditions",
        BookletSectionType.TechnicalScopeAndSpecifications => "Chapter 2: Technical Scope and Specifications",
        BookletSectionType.QualificationRequirements => "Chapter 3: Qualification Requirements",
        BookletSectionType.EvaluationCriteria => "Chapter 4: Evaluation Criteria",
        BookletSectionType.FinancialTerms => "Chapter 5: Financial Terms",
        BookletSectionType.ContractualTerms => "Chapter 6: Contractual Terms",
        BookletSectionType.LocalContentRequirements => "Chapter 7: Local Content Requirements",
        BookletSectionType.AppendicesAndForms => "Chapter 8: Appendices and Forms",
        _ => "Undefined Chapter"
    };

    private static string GetGuidanceNotesAr(BookletSectionType type) => type switch
    {
        BookletSectionType.GeneralTermsAndConditions => "يتضمن هذا الباب الشروط والأحكام العامة المنظمة للمنافسة وفقاً لنظام المنافسات والمشتريات الحكومية ولائحته التنفيذية.",
        BookletSectionType.TechnicalScopeAndSpecifications => "يتضمن هذا الباب وصف نطاق العمل والمواصفات الفنية المطلوبة بشكل تفصيلي ودقيق.",
        BookletSectionType.QualificationRequirements => "يتضمن هذا الباب الشروط والمتطلبات اللازم توفرها في المتنافسين للمشاركة في المنافسة.",
        BookletSectionType.EvaluationCriteria => "يتضمن هذا الباب معايير التقييم الفني والمالي وأوزانها النسبية.",
        BookletSectionType.FinancialTerms => "يتضمن هذا الباب الشروط المالية وطريقة تقديم العروض المالية وجداول الأسعار.",
        BookletSectionType.ContractualTerms => "يتضمن هذا الباب الشروط التعاقدية ومدة العقد والضمانات المطلوبة.",
        BookletSectionType.LocalContentRequirements => "يتضمن هذا الباب متطلبات المحتوى المحلي وفقاً لسياسة هيئة المحتوى المحلي والمشتريات الحكومية.",
        BookletSectionType.AppendicesAndForms => "يتضمن هذا الباب النماذج والملاحق المطلوب تعبئتها وتقديمها مع العرض.",
        _ => ""
    };

    private static string GetGuidanceNotesEn(BookletSectionType type) => type switch
    {
        BookletSectionType.GeneralTermsAndConditions => "This chapter contains the general terms and conditions governing the tender in accordance with the Government Tenders and Procurement Law.",
        BookletSectionType.TechnicalScopeAndSpecifications => "This chapter contains a detailed description of the scope of work and required technical specifications.",
        BookletSectionType.QualificationRequirements => "This chapter contains the conditions and requirements that bidders must meet to participate.",
        BookletSectionType.EvaluationCriteria => "This chapter contains the technical and financial evaluation criteria and their relative weights.",
        BookletSectionType.FinancialTerms => "This chapter contains the financial terms, bid submission method, and price schedules.",
        BookletSectionType.ContractualTerms => "This chapter contains the contractual terms, contract duration, and required guarantees.",
        BookletSectionType.LocalContentRequirements => "This chapter contains local content requirements as per LCGPA policy.",
        BookletSectionType.AppendicesAndForms => "This chapter contains the forms and appendices to be completed and submitted with the bid.",
        _ => ""
    };

    private static string GetDefaultSectionContent(BookletSectionType sectionType, TenderType tenderType)
    {
        return sectionType switch
        {
            BookletSectionType.GeneralTermsAndConditions => @"
<h3>١. تعريفات</h3>
<p>يُقصد بالمصطلحات التالية أينما وردت في هذه الكراسة المعاني المبينة قرين كل منها ما لم يقتضِ السياق خلاف ذلك:</p>
<ul>
<li><strong>الجهة الحكومية:</strong> [اسم الجهة]</li>
<li><strong>المنافسة:</strong> الإجراء الذي تتبعه الجهة الحكومية للحصول على الأعمال والمشتريات.</li>
<li><strong>الكراسة:</strong> كراسة الشروط والمواصفات الخاصة بهذه المنافسة.</li>
<li><strong>المتنافس:</strong> كل شخص طبيعي أو اعتباري يتقدم بعرضه للمنافسة.</li>
<li><strong>العرض:</strong> ما يقدمه المتنافس من مستندات فنية ومالية.</li>
</ul>

<h3>٢. نطاق التطبيق</h3>
<p>تسري أحكام هذه الكراسة على جميع المتنافسين المشاركين في هذه المنافسة، وتُعد جزءاً لا يتجزأ من العقد الذي سيُبرم مع المتنافس الفائز.</p>

<h3>٣. الأنظمة واللوائح المعمول بها</h3>
<p>تخضع هذه المنافسة لأحكام:</p>
<ul>
<li>نظام المنافسات والمشتريات الحكومية الصادر بالمرسوم الملكي رقم (م/١٢٨) وتاريخ ١٤٤٠/١١/١٣هـ.</li>
<li>اللائحة التنفيذية لنظام المنافسات والمشتريات الحكومية.</li>
<li>الأنظمة واللوائح والقرارات ذات العلاقة.</li>
</ul>

<h3>٤. شروط المشاركة</h3>
<p>يُشترط في المتنافس ما يلي:</p>
<ul>
<li>أن يكون مسجلاً في منصة اعتماد.</li>
<li>أن يكون مصنفاً في المجال المطلوب (إن كان التصنيف مطلوباً).</li>
<li>أن يكون سارياً سجله التجاري.</li>
<li>أن يكون ملتزماً بسداد الزكاة والضرائب.</li>
<li>أن يكون مسجلاً في نظام التأمينات الاجتماعية.</li>
</ul>",

            BookletSectionType.TechnicalScopeAndSpecifications => @"
<h3>١. وصف المشروع</h3>
<p>[يُرجى إدخال وصف تفصيلي للمشروع ونطاق العمل المطلوب]</p>

<h3>٢. المواصفات الفنية</h3>
<p>[يُرجى إدخال المواصفات الفنية التفصيلية]</p>

<h3>٣. المخرجات المتوقعة</h3>
<p>[يُرجى تحديد المخرجات والتسليمات المتوقعة]</p>

<h3>٤. الجدول الزمني</h3>
<p>[يُرجى تحديد الجدول الزمني للتنفيذ]</p>

<h3>٥. معايير الجودة</h3>
<p>يجب أن تتوافق جميع الأعمال والمنتجات مع المعايير والمواصفات القياسية المعتمدة من هيئة المواصفات والمقاييس والجودة السعودية (SASO).</p>",

            BookletSectionType.QualificationRequirements => @"
<h3>١. المتطلبات الإدارية</h3>
<ul>
<li>سجل تجاري ساري المفعول.</li>
<li>شهادة الزكاة والدخل سارية المفعول.</li>
<li>شهادة التأمينات الاجتماعية سارية المفعول.</li>
<li>شهادة التصنيف (إن كانت مطلوبة).</li>
</ul>

<h3>٢. المتطلبات الفنية</h3>
<ul>
<li>خبرة لا تقل عن [عدد] سنوات في مجال المنافسة.</li>
<li>تنفيذ [عدد] مشاريع مماثلة على الأقل خلال الخمس سنوات الأخيرة.</li>
<li>توفر الكوادر الفنية المؤهلة.</li>
</ul>

<h3>٣. المتطلبات المالية</h3>
<ul>
<li>القوائم المالية المدققة لآخر ثلاث سنوات.</li>
<li>خطاب من البنك يفيد بالملاءة المالية.</li>
</ul>",

            BookletSectionType.EvaluationCriteria => @"
<h3>١. منهجية التقييم</h3>
<p>يتم تقييم العروض على مرحلتين:</p>
<ul>
<li><strong>التقييم الفني:</strong> بوزن نسبي [٦٠]%</li>
<li><strong>التقييم المالي:</strong> بوزن نسبي [٤٠]%</li>
</ul>

<h3>٢. معايير التقييم الفني</h3>
<p>[سيتم تعبئة هذا القسم تلقائياً من شجرة معايير التقييم]</p>

<h3>٣. معايير التقييم المالي</h3>
<p>[سيتم تعبئة هذا القسم تلقائياً من شجرة معايير التقييم]</p>

<h3>٤. الحد الأدنى للنجاح الفني</h3>
<p>يجب ألا تقل الدرجة الفنية عن [٦٠]% للتأهل للتقييم المالي.</p>",

            BookletSectionType.FinancialTerms => @"
<h3>١. تقديم العرض المالي</h3>
<p>يجب تقديم العرض المالي في مظروف مستقل ومختوم، ويتضمن:</p>
<ul>
<li>جدول الكميات والأسعار مفصلاً.</li>
<li>القيمة الإجمالية شاملة ضريبة القيمة المضافة.</li>
</ul>

<h3>٢. العملة</h3>
<p>تُقدم جميع الأسعار بالريال السعودي.</p>

<h3>٣. صلاحية العرض</h3>
<p>يجب أن يكون العرض سارياً لمدة لا تقل عن (٩٠) يوماً من تاريخ فتح المظاريف.</p>

<h3>٤. الضمان الابتدائي</h3>
<p>يجب تقديم ضمان ابتدائي بنسبة (١-٢)% من قيمة العرض.</p>",

            BookletSectionType.ContractualTerms => @"
<h3>١. مدة العقد</h3>
<p>مدة العقد [يُحدد] من تاريخ توقيعه، قابلة للتجديد وفقاً لأحكام النظام.</p>

<h3>٢. الضمان النهائي</h3>
<p>يلتزم المتعاقد بتقديم ضمان نهائي بنسبة (٥)% من قيمة العقد.</p>

<h3>٣. الغرامات</h3>
<p>في حال التأخير عن التنفيذ، تُفرض غرامة تأخير وفقاً لأحكام نظام المنافسات والمشتريات الحكومية.</p>

<h3>٤. حقوق الملكية الفكرية</h3>
<p>تؤول جميع حقوق الملكية الفكرية للمخرجات إلى الجهة الحكومية.</p>

<h3>٥. السرية</h3>
<p>يلتزم المتعاقد بالمحافظة على سرية جميع المعلومات والبيانات التي يطلع عليها أثناء تنفيذ العقد.</p>",

            BookletSectionType.LocalContentRequirements => @"
<h3>١. التزامات المحتوى المحلي</h3>
<p>يلتزم المتنافس بتحقيق نسبة المحتوى المحلي المطلوبة وفقاً لسياسة هيئة المحتوى المحلي والمشتريات الحكومية (LCGPA).</p>

<h3>٢. النسبة المستهدفة</h3>
<p>النسبة المستهدفة للمحتوى المحلي: [يُحدد]%</p>

<h3>٣. آلية الاحتساب</h3>
<p>يتم احتساب نسبة المحتوى المحلي وفقاً للمعايير المعتمدة من الهيئة.</p>

<h3>٤. المستندات المطلوبة</h3>
<ul>
<li>شهادة المحتوى المحلي سارية المفعول.</li>
<li>خطة تحقيق المحتوى المحلي.</li>
<li>قائمة الموردين المحليين.</li>
</ul>",

            BookletSectionType.AppendicesAndForms => @"
<h3>الملاحق والنماذج</h3>
<p>يجب على المتنافس تعبئة وتقديم النماذج التالية:</p>

<h4>نموذج (١): خطاب التقديم</h4>
<p>[نموذج خطاب التقديم الرسمي]</p>

<h4>نموذج (٢): إقرار بعدم وجود تعارض مصالح</h4>
<p>[نموذج الإقرار]</p>

<h4>نموذج (٣): قائمة المشاريع المماثلة</h4>
<p>[نموذج قائمة المشاريع]</p>

<h4>نموذج (٤): السيرة الذاتية للفريق الفني</h4>
<p>[نموذج السيرة الذاتية]</p>

<h4>نموذج (٥): جدول الكميات والأسعار</h4>
<p>[نموذج جدول الأسعار]</p>",

            _ => "<p>[محتوى القسم]</p>"
        };
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
