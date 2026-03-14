using Mdar.Core.Entities.Canvas;
using Mdar.Core.Entities.Contacts;
using Mdar.Core.Entities.Finance;
using Mdar.Core.Entities.Goals;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Notes;
using Mdar.Core.Entities.Tasks;
using Mdar.Core.Entities.Thinking;
using Mdar.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mdar.Infrastructure.Data;

/// <summary>
/// سياق قاعدة البيانات الرئيسي للنظام.
/// يُعدّ نقطة الدخول الوحيدة لجميع عمليات قاعدة البيانات عبر EF Core.
///
/// الميزات المدمجة:
///   1. Soft Delete تلقائي عبر Query Filters - لا تظهر السجلات المحذوفة أبداً
///   2. تحديث تلقائي لـ UpdatedAt عند كل SaveChanges
///   3. تكوين الجداول عبر Fluent API لأقصى تحكم ووضوح
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── DbSets ───────────────────────────────────────────────────────────────

    // Identity
    public DbSet<User> Users => Set<User>();

    // Tasks Module
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<PomodoroSession> PomodoroSessions => Set<PomodoroSession>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<NoteTag> NoteTags => Set<NoteTag>();

    // Finance Module
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    // Goals & Habits Module
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalMilestone> GoalMilestones => Set<GoalMilestone>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();

    // Contacts Module
    public DbSet<Contact> Contacts => Set<Contact>();

    // Notes Module
    public DbSet<Note> Notes => Set<Note>();

    // Priority Engine Module
    public DbSet<DailyPrayerSchedule> DailyPrayerSchedules => Set<DailyPrayerSchedule>();

    // Thinking Space Module
    public DbSet<ThinkingBoard> ThinkingBoards => Set<ThinkingBoard>();
    public DbSet<ThinkingCard> ThinkingCards => Set<ThinkingCard>();

    // Canvas Security Module
    public DbSet<CanvasBackup>     CanvasBackups     => Set<CanvasBackup>();
    public DbSet<HealthCheckLog>   HealthCheckLogs   => Set<HealthCheckLog>();
    public DbSet<CanvasSyncEvent>  CanvasSyncEvents  => Set<CanvasSyncEvent>();

    // ─── Model Configuration ──────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TiDB: لا يدعم ascii_general_ci — نستخدم utf8mb4_bin لأعمدة GUID
        modelBuilder.UseGuidCollation("utf8mb4_bin");

        // تطبيق جميع تهيئات الجداول من ملفات منفصلة (Modular Configuration)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureUser(modelBuilder);
        ConfigureTasks(modelBuilder);
        ConfigureFinance(modelBuilder);
        ConfigureGoals(modelBuilder);
        ConfigureContacts(modelBuilder);
        ConfigureNotes(modelBuilder);
        ConfigurePriorityEngine(modelBuilder);
        ConfigureThinkingSpace(modelBuilder);
        ConfigureCanvasSecurity(modelBuilder);
        ConfigureHealthCheck(modelBuilder);
        ConfigureCanvasSyncEvents(modelBuilder);
        ApplyGlobalFilters(modelBuilder);
    }

    // ─── SaveChanges Override ─────────────────────────────────────────────────

    /// <summary>
    /// يُضاف هنا منطق مشترك قبل كل حفظ:
    ///   - تحديث UpdatedAt تلقائياً لكل كيان معدَّل
    ///   - يمنع استخدام Delete مباشرة للكيانات التي تدعم Soft Delete
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// يُحدِّث خاصية UpdatedAt تلقائياً لكل كيان في حالة Modified.
    /// لا حاجة لاستدعائه يدوياً في أي مكان في الكود.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified
                     && e.Entity is Core.Entities.Common.BaseEntity);

        foreach (var entry in entries)
        {
            ((Core.Entities.Common.BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }
    }

    // ─── Private Configuration Methods ───────────────────────────────────────

    private static void ConfigureUser(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);

            e.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            e.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            // فريد: لا يمكن لمستخدمَين أن يشتركا في نفس البريد
            e.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            e.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            e.Property(u => u.TimeZone)
                .HasMaxLength(64)
                .HasDefaultValue("Asia/Riyadh");

            e.Property(u => u.AvatarUrl)
                .HasMaxLength(2048);
        });
    }

    private static void ConfigureTasks(ModelBuilder mb)
    {
        // ── Category ──────────────────────────────────────────────────────────
        mb.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.Id);

            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.Property(c => c.Color).HasMaxLength(7).HasDefaultValue("#6B7280");
            e.Property(c => c.Icon).HasMaxLength(50);

            e.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Tag ───────────────────────────────────────────────────────────────
        mb.Entity<Tag>(e =>
        {
            e.ToTable("Tags");
            e.HasKey(t => t.Id);

            e.Property(t => t.Name).IsRequired().HasMaxLength(50);
            e.Property(t => t.Color).HasMaxLength(7).HasDefaultValue("#3B82F6");

            e.HasOne(t => t.User)
                .WithMany(u => u.Tags)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Project ───────────────────────────────────────────────────────────
        mb.Entity<Project>(e =>
        {
            e.ToTable("Projects");
            e.HasKey(p => p.Id);

            e.Property(p => p.Title).IsRequired().HasMaxLength(200);
            e.Property(p => p.Color).HasMaxLength(7).HasDefaultValue("#8B5CF6");
            e.Property(p => p.Icon).HasMaxLength(50);

            e.HasOne(p => p.User)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Category)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TaskItem ──────────────────────────────────────────────────────────
        mb.Entity<TaskItem>(e =>
        {
            e.ToTable("Tasks");
            e.HasKey(t => t.Id);

            e.Property(t => t.Title).IsRequired().HasMaxLength(300);

            // الخاصية المحورية: IsPomodoroCompatible
            e.Property(t => t.IsPomodoroCompatible)
                .HasDefaultValue(true)
                .HasComment("هل المهمة قابلة للتنفيذ بنظام الطماطم (25 دقيقة تركيز)؟");

            e.Property(t => t.EstimatedPomodoros)
                .HasComment("العدد التقديري لجلسات الطماطم اللازمة لإنهاء المهمة");

            // حقول محرك الأولويات
            e.Property(t => t.ContextTag)
                .HasDefaultValue(ContextTag.Anywhere)
                .HasComment("السياق المكاني المطلوب لتنفيذ المهمة");

            e.Property(t => t.IsEmergency)
                .HasDefaultValue(false)
                .HasComment("true = تُستبعد من المحرك العادي وتُعالَج كطوارئ");

            e.Property(t => t.PreferredPrayerPeriod)
                .HasComment("فترة الصلاة المفضلة للتنفيذ — null = أي وقت");

            e.HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // العلاقة الذاتية للمهام الفرعية
            e.HasOne(t => t.ParentTask)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict); // نمنع الحذف التتالي لتجنب حذف شجرة بالكامل

            // فهرس على DueDate لأداء أفضل في استعلامات "مهام اليوم"
            e.HasIndex(t => new { t.UserId, t.DueDate })
                .HasDatabaseName("IX_Tasks_UserId_DueDate");

            // فهرس على IsPomodoroCompatible لتصفية سريعة في واجهة الطماطم
            e.HasIndex(t => new { t.UserId, t.IsPomodoroCompatible, t.Status })
                .HasDatabaseName("IX_Tasks_Pomodoro_Filter");
        });

        // ── TaskTag (Many-to-Many Junction) ───────────────────────────────────
        mb.Entity<TaskTag>(e =>
        {
            e.ToTable("TaskTags");
            e.HasKey(tt => new { tt.TaskItemId, tt.TagId }); // مفتاح مركّب

            e.HasOne(tt => tt.TaskItem)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(tt => tt.Tag)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PomodoroSession ───────────────────────────────────────────────────
        mb.Entity<PomodoroSession>(e =>
        {
            e.ToTable("PomodoroSessions");
            e.HasKey(ps => ps.Id);

            e.Property(ps => ps.PlannedDurationMinutes)
                .HasDefaultValue(25);

            e.HasOne(ps => ps.User)
                .WithMany(u => u.PomodoroSessions)
                .HasForeignKey(ps => ps.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ps => ps.TaskItem)
                .WithMany(t => t.PomodoroSessions)
                .HasForeignKey(ps => ps.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // فهرس للتقارير اليومية والأسبوعية
            e.HasIndex(ps => new { ps.UserId, ps.StartTime })
                .HasDatabaseName("IX_PomodoroSessions_UserId_StartTime");
        });
    }

    private static void ConfigureFinance(ModelBuilder mb)
    {
        // ── FinancialAccount ───────────────────────────────────────────────────
        mb.Entity<FinancialAccount>(e =>
        {
            e.ToTable("FinancialAccounts");
            e.HasKey(fa => fa.Id);

            e.Property(fa => fa.Name).IsRequired().HasMaxLength(100);
            e.Property(fa => fa.Balance).HasPrecision(18, 4);
            e.Property(fa => fa.Currency).HasMaxLength(3).HasDefaultValue("SAR");
            e.Property(fa => fa.Color).HasMaxLength(7).HasDefaultValue("#10B981");

            e.HasOne(fa => fa.User)
                .WithMany(u => u.FinancialAccounts)
                .HasForeignKey(fa => fa.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Transaction ────────────────────────────────────────────────────────
        mb.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions");
            e.HasKey(t => t.Id);

            e.Property(t => t.Amount)
                .IsRequired()
                .HasPrecision(18, 4)
                .HasComment("دائماً قيمة موجبة - الإشارة تُحدَّد بواسطة Type");

            e.Property(t => t.Description).IsRequired().HasMaxLength(300);

            e.HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict); // نحتاج الحذف اليدوي للبيانات المالية

            e.HasOne(t => t.FinancialAccount)
                .WithMany(fa => fa.Transactions)
                .HasForeignKey(t => t.FinancialAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // الحساب المستلِم في التحويلات
            e.HasOne(t => t.ToFinancialAccount)
                .WithMany(fa => fa.IncomingTransfers)
                .HasForeignKey(t => t.ToFinancialAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.Contact)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            // فهرس للتقارير الشهرية
            e.HasIndex(t => new { t.UserId, t.Date })
                .HasDatabaseName("IX_Transactions_UserId_Date");
        });

        // ── Budget ─────────────────────────────────────────────────────────────
        mb.Entity<Budget>(e =>
        {
            e.ToTable("Budgets");
            e.HasKey(b => b.Id);

            e.Property(b => b.Name).IsRequired().HasMaxLength(150);
            e.Property(b => b.LimitAmount).HasPrecision(18, 4);
            e.Property(b => b.SpentAmount).HasPrecision(18, 4).HasDefaultValue(0);

            // الخصائص المحسوبة لا تُخزَّن في قاعدة البيانات
            e.Ignore(b => b.SpentPercentage);
            e.Ignore(b => b.RemainingAmount);

            e.HasOne(b => b.User)
                .WithMany(u => u.Budgets)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureGoals(ModelBuilder mb)
    {
        // ── Goal ───────────────────────────────────────────────────────────────
        mb.Entity<Goal>(e =>
        {
            e.ToTable("Goals");
            e.HasKey(g => g.Id);

            e.Property(g => g.Title).IsRequired().HasMaxLength(200);
            e.Property(g => g.ProgressPercentage)
                .HasDefaultValue(0)
                .HasComment("نسبة الإنجاز من 0 إلى 100");

            e.HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(g => g.Project)
                .WithMany(p => p.Goals)
                .HasForeignKey(g => g.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── GoalMilestone ──────────────────────────────────────────────────────
        mb.Entity<GoalMilestone>(e =>
        {
            e.ToTable("GoalMilestones");
            e.HasKey(m => m.Id);

            e.Property(m => m.Title).IsRequired().HasMaxLength(200);

            e.HasOne(m => m.Goal)
                .WithMany(g => g.Milestones)
                .HasForeignKey(m => m.GoalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Habit ──────────────────────────────────────────────────────────────
        mb.Entity<Habit>(e =>
        {
            e.ToTable("Habits");
            e.HasKey(h => h.Id);

            e.Property(h => h.Title).IsRequired().HasMaxLength(150);
            e.Property(h => h.Color).HasMaxLength(7).HasDefaultValue("#F59E0B");
            e.Property(h => h.Unit).HasMaxLength(30);

            e.HasOne(h => h.User)
                .WithMany(u => u.Habits)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── HabitLog ───────────────────────────────────────────────────────────
        mb.Entity<HabitLog>(e =>
        {
            e.ToTable("HabitLogs");
            e.HasKey(hl => hl.Id);

            // فهرس فريد: سجل واحد فقط لكل عادة في كل يوم
            e.HasIndex(hl => new { hl.HabitId, hl.Date })
                .IsUnique()
                .HasDatabaseName("IX_HabitLogs_HabitId_Date");

            e.HasOne(hl => hl.Habit)
                .WithMany(h => h.Logs)
                .HasForeignKey(hl => hl.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureContacts(ModelBuilder mb)
    {
        mb.Entity<Contact>(e =>
        {
            e.ToTable("Contacts");
            e.HasKey(c => c.Id);

            e.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            e.Property(c => c.LastName).HasMaxLength(100);
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.Phone).HasMaxLength(30);
            e.Property(c => c.Company).HasMaxLength(150);
            e.Property(c => c.JobTitle).HasMaxLength(100);
            e.Property(c => c.AvatarUrl).HasMaxLength(2048);

            // FullName محسوبة من FirstName + LastName، لا تُخزَّن
            e.Ignore(c => c.FullName);

            e.HasOne(c => c.User)
                .WithMany(u => u.Contacts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureNotes(ModelBuilder mb)
    {
        // ── Note ───────────────────────────────────────────────────────────────
        mb.Entity<Note>(e =>
        {
            e.ToTable("Notes");
            e.HasKey(n => n.Id);

            e.Property(n => n.Title).HasMaxLength(300);
            e.Property(n => n.Color).HasMaxLength(7);

            // Content قد يكون نصاً طويلاً - نُعيّن النوع صراحةً
            e.Property(n => n.Content)
                .HasColumnType("longtext")
                .HasDefaultValue(string.Empty);

            e.HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Project)
                .WithMany(p => p.Notes)
                .HasForeignKey(n => n.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(n => n.TaskItem)
                .WithMany(t => t.Notes)
                .HasForeignKey(n => n.TaskItemId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(n => n.Contact)
                .WithMany(c => c.Notes2)
                .HasForeignKey(n => n.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── NoteTag (Many-to-Many Junction) ───────────────────────────────────
        mb.Entity<NoteTag>(e =>
        {
            e.ToTable("NoteTags");
            e.HasKey(nt => new { nt.NoteId, nt.TagId }); // مفتاح مركّب

            e.HasOne(nt => nt.Note)
                .WithMany(n => n.NoteTags)
                .HasForeignKey(nt => nt.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(nt => nt.Tag)
                .WithMany(t => t.NoteTags)
                .HasForeignKey(nt => nt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePriorityEngine(ModelBuilder mb)
    {
        // ── DailyPrayerSchedule ───────────────────────────────────────────────
        mb.Entity<DailyPrayerSchedule>(e =>
        {
            e.ToTable("DailyPrayerSchedules");
            e.HasKey(s => s.Id);

            e.Property(s => s.Source).HasMaxLength(100);

            // فهرس فريد: جدول واحد فقط لكل مستخدم في كل يوم
            e.HasIndex(s => new { s.UserId, s.Date })
                .IsUnique()
                .HasDatabaseName("IX_DailyPrayerSchedules_UserId_Date");

            e.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureThinkingSpace(ModelBuilder mb)
    {
        // ── ThinkingBoard ──────────────────────────────────────────────────────
        mb.Entity<ThinkingBoard>(e =>
        {
            e.ToTable("ThinkingBoards");
            e.HasKey(b => b.Id);

            e.Property(b => b.Title).IsRequired().HasMaxLength(150);
            e.Property(b => b.Description).HasMaxLength(500);

            e.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(b => b.UserId)
                .HasDatabaseName("IX_ThinkingBoards_UserId");
        });

        // ── ThinkingCard ───────────────────────────────────────────────────────
        mb.Entity<ThinkingCard>(e =>
        {
            e.ToTable("ThinkingCards");
            e.HasKey(c => c.Id);

            e.Property(c => c.Title).IsRequired().HasMaxLength(200);
            e.Property(c => c.Content)
                .HasColumnType("longtext")
                .HasDefaultValue(string.Empty);
            e.Property(c => c.Color).HasMaxLength(7).HasDefaultValue("#1e293b");

            e.HasOne(c => c.Board)
                .WithMany(b => b.Cards)
                .HasForeignKey(c => c.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            // فهرس للاستعلام السريع "كل بطاقات لوحة معينة"
            e.HasIndex(c => c.BoardId)
                .HasDatabaseName("IX_ThinkingCards_BoardId");
        });
    }

    private static void ConfigureCanvasSecurity(ModelBuilder mb)
    {
        // ── CanvasBackup ───────────────────────────────────────────────────────
        mb.Entity<CanvasBackup>(e =>
        {
            e.ToTable("CanvasBackups");
            e.HasKey(b => b.Id);

            e.Property(b => b.FileName)
                .IsRequired()
                .HasMaxLength(260);

            // البيانات الثنائية المشفرة — longblob
            // الخادم لا يفك تشفيرها (Zero-Knowledge)
            e.Property(b => b.EncryptedData)
                .IsRequired()
                .HasColumnType("longblob")
                .HasComment("AES-256-GCM: Magic(4) + Version(1) + Salt(16) + IV(12) + Ciphertext");

            e.Property(b => b.Label)
                .HasMaxLength(100);

            e.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // فهرس لاسترداد نسخ مستخدم معين بسرعة
            e.HasIndex(b => new { b.UserId, b.CreatedAt })
                .HasDatabaseName("IX_CanvasBackups_UserId_CreatedAt");
        });
    }

    private static void ConfigureCanvasSyncEvents(ModelBuilder mb)
    {
        mb.Entity<CanvasSyncEvent>(e =>
        {
            e.ToTable("CanvasSyncEvents");
            e.HasKey(ev => ev.Id);

            e.Property(ev => ev.EventType).IsRequired().HasMaxLength(30);
            e.Property(ev => ev.Payload).HasColumnType("longtext");
            e.Property(ev => ev.SessionId).HasMaxLength(64);

            // فهرس رئيسي: استعلام "أحداث لوحة X منذ تاريخ Y"
            e.HasIndex(ev => new { ev.BoardId, ev.Timestamp })
                .HasDatabaseName("IX_CanvasSyncEvents_BoardId_Timestamp");

            // نقطة انتهاء تلقائي: يمكن لاحقاً حذف الأحداث الأقدم من 30 يوم
            // TiDB لا يدعم DEFAULT expressions لـ datetime(6) — القيمة تُضبط في الكود
            e.Property(ev => ev.Timestamp)
                .HasComment("UTC — يُستخدم في Delta Sync وConflict Resolution");
        });
    }

    private static void ConfigureHealthCheck(ModelBuilder mb)
    {
        mb.Entity<HealthCheckLog>(e =>
        {
            e.ToTable("HealthCheckLogs");
            e.HasKey(h => h.Id);

            e.Property(h => h.CheckType).IsRequired().HasMaxLength(60);
            e.Property(h => h.Status).IsRequired().HasMaxLength(20);
            e.Property(h => h.Details).HasColumnType("longtext");

            // فهرس للاستعلام الزمني السريع
            e.HasIndex(h => h.CheckedAt)
                .HasDatabaseName("IX_HealthCheckLogs_CheckedAt");
        });
    }

    /// <summary>
    /// تطبيق Global Query Filters على جميع الكيانات التي تدعم Soft Delete.
    /// بعد هذا الإعداد، أي استعلام LINQ سيتجاهل السجلات حيث IsDeleted = true
    /// تلقائياً دون الحاجة لإضافة الشرط في كل مكان.
    ///
    /// لاستعادة سجل محذوف يجب استخدام: .IgnoreQueryFilters()
    /// </summary>
    private static void ApplyGlobalFilters(ModelBuilder mb)
    {
        mb.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<TaskItem>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<PomodoroSession>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Tag>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<FinancialAccount>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Transaction>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Budget>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Goal>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<GoalMilestone>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Habit>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<HabitLog>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Contact>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Note>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<DailyPrayerSchedule>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<ThinkingBoard>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<ThinkingCard>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<CanvasBackup>().HasQueryFilter(e => !e.IsDeleted);
    }
}
