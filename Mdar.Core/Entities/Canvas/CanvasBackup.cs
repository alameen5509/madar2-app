using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;

namespace Mdar.Core.Entities.Canvas;

/// <summary>
/// نسخة احتياطية مشفرة من لوحة InfiniteCanvas.
///
/// مبدأ Zero-Knowledge:
///   النظام يخزّن البيانات المشفرة فقط كـ varbinary.
///   مفتاح فك التشفير (كلمة المرور) يبقى عند المستخدم حصرياً
///   ولا يُرسَل إلى الخادم في أي وقت.
///
/// تنسيق الملف (.mdar):
///   [0..3]  Magic = "MDAR"  (4 bytes)
///   [4]     Version = 0x02  (1 byte)
///   [5..20] Salt    = 16 bytes  (PBKDF2)
///   [21..32] IV      = 12 bytes  (AES-GCM nonce)
///   [33..]  Ciphertext = AES-256-GCM encrypted JSON
/// </summary>
public class CanvasBackup : BaseEntity
{
    // ── مالك النسخة ───────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم صاحب النسخة الاحتياطية</summary>
    public Guid UserId { get; set; }

    /// <summary>navigation property — للـ JOIN عند الحاجة</summary>
    public User User { get; set; } = null!;

    // ── بيانات الملف ──────────────────────────────────────────────────────────

    /// <summary>اسم الملف الأصلي (مثال: مدار-backup-2026-03-14.mdar)</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// البيانات الثنائية المشفرة بالكامل (Magic + Version + Salt + IV + Ciphertext).
    /// يُخزَّن كـ varbinary(max) في SQL Server.
    /// لا يُعالَج من جانب الخادم — يُرسَل للعميل كما هو عند التنزيل.
    /// </summary>
    public byte[] EncryptedData { get; set; } = Array.Empty<byte>();

    /// <summary>حجم الملف بالبايت — يُعرض في قائمة النسخ الاحتياطية</summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// ملاحظة اختيارية من المستخدم لتمييز النسخة
    /// (مثال: "قبل التحديث الكبير"، "لوحة مشروع ألفا")
    /// </summary>
    public string? Label { get; set; }
}
