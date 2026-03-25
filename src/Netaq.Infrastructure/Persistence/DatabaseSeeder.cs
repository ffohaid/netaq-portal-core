using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;

namespace Netaq.Infrastructure.Persistence;

/// <summary>
/// Seeds initial system data: organization, users with different roles,
/// committees, permission matrix, workflow templates, AI configuration,
/// sample tenders, and global booklet templates (21 templates across 7 categories).
/// Only runs on first startup when database is empty.
/// </summary>
public static class DatabaseSeeder
{
    // Fixed IDs for referencing across seed data
    private static readonly Guid OrgId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ManagerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CoordinatorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ChairId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid MemberId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ViewerId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid AuditorId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Only seed if no organizations exist
        if (await context.Organizations.IgnoreQueryFilters().AnyAsync())
        {
            await SeedTemplatesIfNeeded(context);
            return;
        }

        // ========== 1. Organization ==========
        var organization = new Organization
        {
            Id = OrgId,
            NameAr = "وزارة المالية",
            NameEn = "Ministry of Finance",
            DescriptionAr = "وزارة المالية - المملكة العربية السعودية",
            DescriptionEn = "Ministry of Finance - Kingdom of Saudi Arabia",
            Email = "admin@netaq.pro",
            ActiveAuthProvider = AuthProviderType.CustomAuth,
            IsOtpEnabled = false,
            ShowPlatformLogo = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Organizations.Add(organization);

        // ========== 2. Users (7 users with different roles) ==========
        var users = CreateUsers();
        foreach (var user in users)
            context.Users.Add(user);

        // ========== 3. Permission Matrix (all roles × all phases) ==========
        var permissions = CreatePermissionMatrix();
        foreach (var perm in permissions)
            context.PermissionMatrices.Add(perm);

        // ========== 4. Committees ==========
        var committees = CreateCommittees();
        foreach (var committee in committees)
        {
            context.Committees.Add(committee);
            foreach (var member in committee.Members)
                context.CommitteeMembers.Add(member);
        }

        // ========== 5. Workflow Templates ==========
        var workflows = CreateWorkflowTemplates();
        foreach (var wf in workflows)
        {
            context.WorkflowTemplates.Add(wf);
            foreach (var step in wf.Steps)
                context.WorkflowSteps.Add(step);
        }

        // ========== 6. AI Configuration ==========
        var aiConfig = new AiConfiguration
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            ProviderName = "Gemini",
            ModelName = "gemini-2.5-flash",
            Endpoint = "https://generativelanguage.googleapis.com/v1beta",
            ApiKeyEncrypted = "", // To be configured by admin
            IsActive = true,
            MaxTokens = 4096,
            Temperature = 0.3,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        };
        context.AiConfigurations.Add(aiConfig);

        // ========== 7. Sample Tenders ==========
        var tenders = CreateSampleTenders();
        foreach (var tender in tenders)
            context.Tenders.Add(tender);

        // ========== 8. System Settings ==========
        var settings = CreateSystemSettings();
        foreach (var setting in settings)
            context.SystemSettings.Add(setting);

        await context.SaveChangesAsync();

        // ========== 9. Booklet Templates ==========
        await SeedTemplatesIfNeeded(context);
    }

    // ==================== Users ====================
    private static List<User> CreateUsers()
    {
        var users = new List<User>();
        var defaultPassword = "NetaqAdmin@2026!";

        var userData = new[]
        {
            (AdminId, "مدير النظام", "System Administrator", "admin@netaq.pro", OrganizationRole.SystemAdmin, "ar"),
            (ManagerId, "أحمد بن محمد العتيبي", "Ahmed Al-Otaibi", "ahmed.manager@netaq.pro", OrganizationRole.DepartmentManager, "ar"),
            (CoordinatorId, "سارة بنت عبدالله القحطاني", "Sarah Al-Qahtani", "sarah.coordinator@netaq.pro", OrganizationRole.Coordinator, "ar"),
            (ChairId, "د. خالد بن فهد الشمري", "Dr. Khalid Al-Shammari", "khalid.chair@netaq.pro", OrganizationRole.CommitteeChair, "ar"),
            (MemberId, "فاطمة بنت سعد الدوسري", "Fatimah Al-Dosari", "fatimah.member@netaq.pro", OrganizationRole.CommitteeMember, "ar"),
            (ViewerId, "نورة بنت علي الحربي", "Noura Al-Harbi", "noura.viewer@netaq.pro", OrganizationRole.Viewer, "ar"),
            (AuditorId, "عبدالرحمن بن صالح الغامدي", "Abdulrahman Al-Ghamdi", "abdulrahman.auditor@netaq.pro", OrganizationRole.Viewer, "ar"),
        };

        foreach (var (id, nameAr, nameEn, email, role, locale) in userData)
        {
            var (hash, salt) = HashPassword(defaultPassword);
            users.Add(new User
            {
                Id = id,
                OrganizationId = OrgId,
                FullNameAr = nameAr,
                FullNameEn = nameEn,
                Email = email,
                Role = role,
                Status = UserStatus.Active,
                Locale = locale,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            });
        }

        return users;
    }

    // ==================== Permission Matrix ====================
    private static List<PermissionMatrix> CreatePermissionMatrix()
    {
        var permissions = new List<PermissionMatrix>();

        // Define role permissions per phase
        var rolePermissions = new Dictionary<OrganizationRole, (bool view, bool create, bool edit, bool delete, bool approve, bool reject, bool deleg, bool export)>
        {
            [OrganizationRole.SystemAdmin] = (true, true, true, true, true, true, true, true),
            [OrganizationRole.DepartmentManager] = (true, true, true, false, true, true, true, true),
            [OrganizationRole.Coordinator] = (true, true, true, false, false, false, false, true),
            [OrganizationRole.CommitteeChair] = (true, false, true, false, true, true, true, true),
            [OrganizationRole.CommitteeMember] = (true, false, true, false, false, false, false, false),
            [OrganizationRole.Viewer] = (true, false, false, false, false, false, false, true),
            [OrganizationRole.LegalAdvisor] = (true, false, true, false, false, false, false, true),
        };

        foreach (TenderPhase phase in Enum.GetValues<TenderPhase>())
        {
            foreach (var (role, perms) in rolePermissions)
            {
                permissions.Add(new PermissionMatrix
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = OrgId,
                    TenderPhase = phase,
                    UserRole = role,
                    CanView = perms.view,
                    CanCreate = perms.create,
                    CanEdit = perms.edit,
                    CanDelete = perms.delete,
                    CanApprove = perms.approve,
                    CanReject = perms.reject,
                    CanDelegate = perms.deleg,
                    CanExport = perms.export,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = AdminId
                });
            }
        }

        return permissions;
    }

    // ==================== Committees ====================
    private static List<Committee> CreateCommittees()
    {
        var committees = new List<Committee>();

        // 1. Permanent Tender Opening Committee
        var openingCommittee = new Committee
        {
            Id = Guid.Parse("c1111111-1111-1111-1111-111111111111"),
            OrganizationId = OrgId,
            NameAr = "لجنة فتح المظاريف",
            NameEn = "Tender Opening Committee",
            Type = CommitteeType.Permanent,
            PurposeAr = "لجنة دائمة مختصة بفتح مظاريف العروض الفنية والمالية للمنافسات وفقاً لنظام المنافسات والمشتريات الحكومية",
            PurposeEn = "Permanent committee responsible for opening technical and financial bid envelopes in accordance with the Government Tenders and Procurement Law",
            IsActive = true,
            FormedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        };
        openingCommittee.Members = new List<CommitteeMember>
        {
            new() { Id = Guid.NewGuid(), CommitteeId = openingCommittee.Id, UserId = ChairId, Role = CommitteeMemberRole.Chair, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), CommitteeId = openingCommittee.Id, UserId = MemberId, Role = CommitteeMemberRole.Member, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), CommitteeId = openingCommittee.Id, UserId = CoordinatorId, Role = CommitteeMemberRole.Secretary, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
        };
        committees.Add(openingCommittee);

        // 2. Permanent Technical Evaluation Committee
        var techCommittee = new Committee
        {
            Id = Guid.Parse("c2222222-2222-2222-2222-222222222222"),
            OrganizationId = OrgId,
            NameAr = "لجنة الفحص والتقييم الفني",
            NameEn = "Technical Evaluation Committee",
            Type = CommitteeType.Permanent,
            PurposeAr = "لجنة دائمة مختصة بفحص وتقييم العروض الفنية المقدمة في المنافسات الحكومية",
            PurposeEn = "Permanent committee responsible for examining and evaluating technical proposals submitted in government tenders",
            IsActive = true,
            FormedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        };
        techCommittee.Members = new List<CommitteeMember>
        {
            new() { Id = Guid.NewGuid(), CommitteeId = techCommittee.Id, UserId = ChairId, Role = CommitteeMemberRole.Chair, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), CommitteeId = techCommittee.Id, UserId = MemberId, Role = CommitteeMemberRole.Member, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), CommitteeId = techCommittee.Id, UserId = ManagerId, Role = CommitteeMemberRole.Member, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
        };
        committees.Add(techCommittee);

        // 3. Permanent Procurement Review Committee
        var reviewCommittee = new Committee
        {
            Id = Guid.Parse("c3333333-3333-3333-3333-333333333333"),
            OrganizationId = OrgId,
            NameAr = "لجنة مراجعة المشتريات",
            NameEn = "Procurement Review Committee",
            Type = CommitteeType.Permanent,
            PurposeAr = "لجنة دائمة مختصة بمراجعة واعتماد إجراءات المشتريات والتوصيات النهائية",
            PurposeEn = "Permanent committee responsible for reviewing and approving procurement procedures and final recommendations",
            IsActive = true,
            FormedAt = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        };
        reviewCommittee.Members = new List<CommitteeMember>
        {
            new() { Id = Guid.NewGuid(), CommitteeId = reviewCommittee.Id, UserId = ManagerId, Role = CommitteeMemberRole.Chair, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), CommitteeId = reviewCommittee.Id, UserId = ChairId, Role = CommitteeMemberRole.Member, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), CommitteeId = reviewCommittee.Id, UserId = AuditorId, Role = CommitteeMemberRole.Member, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
        };
        committees.Add(reviewCommittee);

        return committees;
    }

    // ==================== Workflow Templates ====================
    private static List<WorkflowTemplate> CreateWorkflowTemplates()
    {
        var templates = new List<WorkflowTemplate>();

        // 1. Tender Approval Workflow
        var tenderWf = new WorkflowTemplate
        {
            Id = Guid.Parse("01111111-1111-1111-1111-111111111111"),
            OrganizationId = OrgId,
            NameAr = "سير عمل اعتماد المنافسة",
            NameEn = "Tender Approval Workflow",
            DescriptionAr = "سير العمل الافتراضي لاعتماد كراسة الشروط والمواصفات",
            DescriptionEn = "Default workflow for approving terms and specifications booklet",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        };
        tenderWf.Steps = new List<WorkflowStep>
        {
            new() { Id = Guid.NewGuid(), WorkflowTemplateId = tenderWf.Id, NameAr = "مراجعة المنسق", NameEn = "Coordinator Review", Order = 1, StepType = WorkflowStepType.Sequential, RequiredRole = OrganizationRole.Coordinator, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), WorkflowTemplateId = tenderWf.Id, NameAr = "اعتماد مدير الإدارة", NameEn = "Department Manager Approval", Order = 2, StepType = WorkflowStepType.Sequential, RequiredRole = OrganizationRole.DepartmentManager, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), WorkflowTemplateId = tenderWf.Id, NameAr = "الاعتماد النهائي", NameEn = "Final Approval", Order = 3, StepType = WorkflowStepType.Sequential, RequiredRole = OrganizationRole.SystemAdmin, CreatedAt = DateTime.UtcNow },
        };
        templates.Add(tenderWf);

        // 2. Evaluation Approval Workflow
        var evalWf = new WorkflowTemplate
        {
            Id = Guid.Parse("02222222-2222-2222-2222-222222222222"),
            OrganizationId = OrgId,
            NameAr = "سير عمل اعتماد التقييم",
            NameEn = "Evaluation Approval Workflow",
            DescriptionAr = "سير العمل لاعتماد تقارير التقييم الفني والمالي",
            DescriptionEn = "Workflow for approving technical and financial evaluation reports",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        };
        evalWf.Steps = new List<WorkflowStep>
        {
            new() { Id = Guid.NewGuid(), WorkflowTemplateId = evalWf.Id, NameAr = "مراجعة رئيس اللجنة", NameEn = "Committee Chair Review", Order = 1, StepType = WorkflowStepType.Sequential, RequiredRole = OrganizationRole.CommitteeChair, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), WorkflowTemplateId = evalWf.Id, NameAr = "اعتماد مدير الإدارة", NameEn = "Department Manager Approval", Order = 2, StepType = WorkflowStepType.Sequential, RequiredRole = OrganizationRole.DepartmentManager, CreatedAt = DateTime.UtcNow },
        };
        templates.Add(evalWf);

        // 3. Inquiry Workflow
        var inquiryWf = new WorkflowTemplate
        {
            Id = Guid.Parse("03333333-3333-3333-3333-333333333333"),
            OrganizationId = OrgId,
            NameAr = "سير عمل الاستفسارات",
            NameEn = "Inquiry Workflow",
            DescriptionAr = "سير العمل لإدارة الاستفسارات والتوضيحات على المنافسات",
            DescriptionEn = "Workflow for managing inquiries and clarifications on tenders",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        };
        inquiryWf.Steps = new List<WorkflowStep>
        {
            new() { Id = Guid.NewGuid(), WorkflowTemplateId = inquiryWf.Id, NameAr = "تعيين المختص", NameEn = "Assign Specialist", Order = 1, StepType = WorkflowStepType.Sequential, RequiredRole = OrganizationRole.Coordinator, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), WorkflowTemplateId = inquiryWf.Id, NameAr = "الرد على الاستفسار", NameEn = "Respond to Inquiry", Order = 2, StepType = WorkflowStepType.Sequential, RequiredRole = OrganizationRole.CommitteeMember, CreatedAt = DateTime.UtcNow },
        };
        templates.Add(inquiryWf);

        return templates;
    }

    // ==================== Sample Tenders ====================
    private static List<Tender> CreateSampleTenders()
    {
        var tenders = new List<Tender>();

        tenders.Add(new Tender
        {
            Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
            OrganizationId = OrgId,
            TitleAr = "منافسة توريد أجهزة حاسب آلي ومعدات تقنية",
            TitleEn = "Computer Equipment and IT Hardware Supply Tender",
            DescriptionAr = "منافسة عامة لتوريد أجهزة حاسب آلي محمولة ومكتبية وشاشات وطابعات ومعدات شبكات لمقر الوزارة الرئيسي والفروع",
            DescriptionEn = "General tender for supply of laptops, desktops, monitors, printers, and network equipment for the ministry headquarters and branches",
            ReferenceNumber = "MOF-2026-001",
            TenderType = TenderType.GeneralSupply,
            Status = TenderStatus.Draft,
            EstimatedValue = 2500000m,
            SubmissionCloseDate = DateTime.UtcNow.AddDays(45),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = AdminId
        });

        tenders.Add(new Tender
        {
            Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
            OrganizationId = OrgId,
            TitleAr = "منافسة خدمات الصيانة والتشغيل للمباني",
            TitleEn = "Building Maintenance and Operations Services Tender",
            DescriptionAr = "منافسة عامة لتقديم خدمات الصيانة والتشغيل الشاملة لمباني الوزارة بما يشمل الصيانة الكهربائية والميكانيكية وأنظمة التكييف",
            DescriptionEn = "General tender for comprehensive building maintenance and operations services including electrical, mechanical, and HVAC systems",
            ReferenceNumber = "MOF-2026-002",
            TenderType = TenderType.GeneralServices,
            Status = TenderStatus.PendingApproval,
            EstimatedValue = 4800000m,
            SubmissionCloseDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            CreatedBy = ManagerId
        });

        tenders.Add(new Tender
        {
            Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
            OrganizationId = OrgId,
            TitleAr = "منافسة تطوير نظام إدارة الموارد البشرية",
            TitleEn = "HR Management System Development Tender",
            DescriptionAr = "منافسة لتطوير وتنفيذ نظام متكامل لإدارة الموارد البشرية يشمل شؤون الموظفين والرواتب والتدريب وتقييم الأداء",
            DescriptionEn = "Tender for development and implementation of an integrated HR management system including personnel, payroll, training, and performance evaluation",
            ReferenceNumber = "MOF-2026-003",
            TenderType = TenderType.InformationTechnology,
            Status = TenderStatus.Draft,
            EstimatedValue = 8500000m,
            SubmissionCloseDate = DateTime.UtcNow.AddDays(60),
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            CreatedBy = CoordinatorId
        });

        tenders.Add(new Tender
        {
            Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
            OrganizationId = OrgId,
            TitleAr = "منافسة استشارات تطوير الخطة الاستراتيجية",
            TitleEn = "Strategic Plan Development Consulting Tender",
            DescriptionAr = "منافسة لتقديم خدمات استشارية لتطوير الخطة الاستراتيجية للوزارة للفترة 2027-2030 بما يتوافق مع رؤية المملكة 2030",
            DescriptionEn = "Tender for consulting services to develop the ministry's strategic plan for 2027-2030 aligned with Saudi Vision 2030",
            ReferenceNumber = "MOF-2026-004",
            TenderType = TenderType.GeneralConsulting,
            Status = TenderStatus.Approved,
            EstimatedValue = 3200000m,
            SubmissionCloseDate = DateTime.UtcNow.AddDays(20),
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            CreatedBy = AdminId
        });

        tenders.Add(new Tender
        {
            Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"),
            OrganizationId = OrgId,
            TitleAr = "منافسة إنشاء مبنى الفرع الجديد - المنطقة الشرقية",
            TitleEn = "New Branch Building Construction - Eastern Province",
            DescriptionAr = "منافسة لإنشاء وتشطيب مبنى الفرع الجديد للوزارة في المنطقة الشرقية بمساحة إجمالية 5000 متر مربع",
            DescriptionEn = "Tender for construction and finishing of the new ministry branch building in the Eastern Province with a total area of 5,000 sqm",
            ReferenceNumber = "MOF-2026-005",
            TenderType = TenderType.GeneralConstruction,
            Status = TenderStatus.Approved,
            EstimatedValue = 15000000m,
            SubmissionCloseDate = DateTime.UtcNow.AddDays(90),
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            CreatedBy = ManagerId
        });

        return tenders;
    }

    // ==================== System Settings ====================
    private static List<SystemSetting> CreateSystemSettings()
    {
        var settings = new List<SystemSetting>();
        var now = DateTime.UtcNow;

        var settingsData = new[]
        {
            ("session.access_token_minutes", "60", "ar", "مدة صلاحية رمز الوصول بالدقائق", "Access token validity in minutes"),
            ("session.refresh_token_hours", "8", "ar", "مدة صلاحية رمز التحديث بالساعات", "Refresh token validity in hours"),
            ("sla.default_response_hours", "48", "ar", "المدة الافتراضية للرد بالساعات", "Default response time in hours"),
            ("sla.escalation_hours", "72", "ar", "مدة التصعيد بالساعات", "Escalation time in hours"),
            ("tender.max_extension_days", "30", "ar", "أقصى مدة تمديد للمنافسة بالأيام", "Maximum tender extension in days"),
            ("tender.min_submission_days", "15", "ar", "أقل مدة لتقديم العروض بالأيام", "Minimum submission period in days"),
            ("notification.email_enabled", "true", "ar", "تفعيل إشعارات البريد الإلكتروني", "Enable email notifications"),
            ("locale.default_language", "ar", "ar", "اللغة الافتراضية للنظام", "Default system language"),
            ("locale.hijri_calendar_enabled", "true", "ar", "تفعيل التقويم الهجري", "Enable Hijri calendar"),
            ("locale.number_format", "ar-SA", "ar", "تنسيق الأرقام", "Number format"),
            ("security.max_login_attempts", "5", "ar", "أقصى عدد محاولات تسجيل الدخول", "Maximum login attempts"),
            ("security.lockout_minutes", "30", "ar", "مدة قفل الحساب بالدقائق", "Account lockout duration in minutes"),
        };

        foreach (var (key, value, locale, descAr, descEn) in settingsData)
        {
            settings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                SettingKey = key,
                SettingValue = value,
                Category = key.Split('.')[0],
                LabelAr = descAr,
                LabelEn = descEn,
                DataType = "string",
                CreatedAt = now,
                CreatedBy = AdminId
            });
        }

        return settings;
    }

    // ==================== Booklet Templates ====================
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
<table border='1' cellpadding='5' cellspacing='0'>
<tr><th>المعيار</th><th>الوزن</th></tr>
<tr><td>منهجية العمل وخطة التنفيذ</td><td>25%</td></tr>
<tr><td>الخبرات والمشاريع المماثلة</td><td>20%</td></tr>
<tr><td>فريق العمل والكوادر الفنية</td><td>15%</td></tr>
</table>",

            BookletSectionType.FinancialTerms => @"
<h3>١. تقديم العرض المالي</h3>
<p>يجب تقديم العرض المالي في مظروف مستقل ومختوم.</p>

<h3>٢. جدول الكميات والأسعار</h3>
<p>يجب تعبئة جدول الكميات والأسعار المرفق بالكامل.</p>

<h3>٣. الضمان الابتدائي</h3>
<p>يجب تقديم ضمان ابتدائي بنسبة (١-٢)% من قيمة العرض المالي.</p>

<h3>٤. شروط الدفع</h3>
<p>يتم الدفع وفقاً للمراحل المحددة في العقد بعد إنجاز كل مرحلة واعتمادها.</p>",

            BookletSectionType.ContractualTerms => @"
<h3>١. مدة العقد</h3>
<p>[يُرجى تحديد مدة العقد]</p>

<h3>٢. الضمان النهائي</h3>
<p>يلتزم المتعاقد بتقديم ضمان نهائي بنسبة (٥)% من قيمة العقد.</p>

<h3>٣. الغرامات</h3>
<p>في حال التأخير عن التنفيذ يتم تطبيق غرامة تأخير بنسبة لا تتجاوز (١٠)% من قيمة العقد.</p>

<h3>٤. فسخ العقد</h3>
<p>يحق للجهة الحكومية فسخ العقد وفقاً لأحكام نظام المنافسات والمشتريات الحكومية.</p>",

            BookletSectionType.LocalContentRequirements => @"
<h3>١. متطلبات المحتوى المحلي</h3>
<p>يلتزم المتنافس بتحقيق نسبة المحتوى المحلي المطلوبة وفقاً لسياسة هيئة المحتوى المحلي والمشتريات الحكومية.</p>

<h3>٢. المستندات المطلوبة</h3>
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
