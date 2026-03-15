using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Contacts;
using Mdar.Core.Entities.Finance;
using Mdar.Core.Entities.Goals;
using Mdar.Core.Entities.Notes;
using Mdar.Core.Entities.Tasks;

namespace Mdar.Core.Entities.Identity;

/// <summary>
/// مستخدم النظام. كل بيانات النظام مرتبطة بمستخدم واحد
/// لأن هذا ERP شخصي (Single-User في الغالب).
/// </summary>
public class User : BaseEntity
{
    /// <summary>الاسم الكامل للمستخدم</summary>
    public required string FullName { get; set; }

    /// <summary>
    /// البريد الإلكتروني - يُستخدم كـ Username للتسجيل.
    /// فريد في النظام.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// كلمة المرور مشفرة بـ BCrypt.
    /// لا تُخزَّن كلمة المرور نصاً صريحاً أبداً.
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>رابط صورة الملف الشخصي (اختياري)</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// المنطقة الزمنية للمستخدم بصيغة IANA.
    /// مثال: "Asia/Riyadh" أو "Africa/Cairo".
    /// يُستخدم لعرض التواريخ بالتوقيت الصحيح.
    /// </summary>
    public string TimeZone { get; set; } = "Asia/Riyadh";

    /// <summary>دور المستخدم في النظام: "user" أو "admin"</summary>
    public string Role { get; set; } = "user";

    // ─── إعدادات نظام الطماطم (Pomodoro Settings) ───────────────────────────

    /// <summary>
    /// مدة جلسة الطماطم المفضلة بالدقائق.
    /// القيمة الافتراضية: 25 دقيقة (الكلاسيكي).
    /// </summary>
    public int PreferredPomodoroMinutes { get; set; } = 25;

    /// <summary>
    /// مدة فترة الاستراحة القصيرة بالدقائق.
    /// القيمة الافتراضية: 5 دقائق.
    /// </summary>
    public int PreferredShortBreakMinutes { get; set; } = 5;

    /// <summary>
    /// مدة فترة الاستراحة الطويلة بالدقائق.
    /// تأتي بعد كل 4 جلسات طماطم.
    /// القيمة الافتراضية: 15 دقيقة.
    /// </summary>
    public int PreferredLongBreakMinutes { get; set; } = 15;

    /// <summary>
    /// عدد جلسات الطماطم قبل أخذ استراحة طويلة.
    /// القيمة الافتراضية: 4 جلسات.
    /// </summary>
    public int PomodorosBeforeLongBreak { get; set; } = 4;

    // ─── Navigation Properties ────────────────────────────────────────────────

    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<TaskItem> Tasks { get; set; } = [];
    public ICollection<PomodoroSession> PomodoroSessions { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<FinancialAccount> FinancialAccounts { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<Habit> Habits { get; set; } = [];
    public ICollection<Contact> Contacts { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
}
