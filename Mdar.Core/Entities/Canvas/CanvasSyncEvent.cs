namespace Mdar.Core.Entities.Canvas;

/// <summary>
/// سجل أحداث المزامنة — Event Log للتزامن السحابي.
///
/// نمط Event Sourcing:
///   كل تغيير على اللوحة يُخزَّن كحدث منفصل.
///   يتيح ذلك:
///     - Delta Sync: جلب التغييرات منذ تاريخ معين
///     - تتبع من غيّر ماذا ومتى
///     - Conflict Resolution بناءً على Timestamp
///
/// أنواع الأحداث (EventType):
///   node_created   — بطاقة جديدة
///   node_deleted   — حذف بطاقة
///   node_moved     — تحريك/تغيير حجم
///   text_changed   — تعديل عنوان أو محتوى
///   connection_added   — ربط بين بطاقتين  [مستقبلاً]
///   connection_removed — حذف ربط          [مستقبلاً]
/// </summary>
public class CanvasSyncEvent
{
    public Guid     Id        { get; set; } = Guid.NewGuid();

    /// <summary>معرّف اللوحة</summary>
    public Guid     BoardId   { get; set; }

    /// <summary>المستخدم الذي أجرى التغيير</summary>
    public Guid     UserId    { get; set; }

    /// <summary>نوع الحدث — يُطابق node_created / node_moved / text_changed / ...</summary>
    public string   EventType { get; set; } = string.Empty;

    /// <summary>البيانات الكاملة للحدث بتنسيق JSON</summary>
    public string   Payload   { get; set; } = string.Empty;

    /// <summary>وقت الحدث بالتوقيت العالمي — يُستخدم في Conflict Resolution</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>معرّف الجلسة — يمنع الـ client من استقبال أحداثه الخاصة</summary>
    public string?  SessionId { get; set; }
}
