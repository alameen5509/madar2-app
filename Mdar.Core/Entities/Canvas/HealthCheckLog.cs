namespace Mdar.Core.Entities.Canvas;

/// <summary>
/// سجل عمليات فحص صحة النظام.
///
/// يُخزِّن نتائج كل دورة فحص مع تفاصيل المشاكل المكتشفة والمُصلَحة.
/// لا يرث BaseEntity لأنه سجل تاريخي غير قابل للحذف اللطيف.
///
/// أنواع الفحص (CheckType):
///   OrphanedConnections — روابط تشير لبطاقات محذوفة
///   OrphanedTasks       — مهام تشير لبطاقات محذوفة
///   CorruptedSnapshots  — لقطات بيانات JSON تالفة
///   DatabaseIntegrity   — DBCC CHECKDB / PRAGMA integrity_check
///   BackupMagicBytes    — نسخ احتياطية مكسورة التنسيق
///   StaleExports        — ملفات تصدير مؤقتة أقدم من 24 ساعة
/// </summary>
public class HealthCheckLog
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public DateTime CheckedAt   { get; set; } = DateTime.UtcNow;

    /// <summary>نوع الفحص — يُطابق نص enum CheckType</summary>
    public string   CheckType   { get; set; } = string.Empty;

    /// <summary>Passed | Warning | Failed</summary>
    public string   Status      { get; set; } = string.Empty;

    /// <summary>عدد المشاكل المكتشفة</summary>
    public int      IssuesFound { get; set; }

    /// <summary>عدد المشاكل التي أصلحها النظام تلقائياً</summary>
    public int      IssuesFixed { get; set; }

    /// <summary>تفاصيل إضافية — JSON أو نص حر</summary>
    public string?  Details     { get; set; }
}

/// <summary>أنواع الفحوصات المتاحة</summary>
public enum CheckType
{
    OrphanedConnections,
    OrphanedTasks,
    CorruptedSnapshots,
    DatabaseIntegrity,
    BackupMagicBytes,
    StaleExports,
}
