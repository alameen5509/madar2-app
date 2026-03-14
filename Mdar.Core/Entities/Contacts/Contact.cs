using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Finance;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Notes;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Contacts;

/// <summary>
/// جهة اتصال - شخص أو جهة يتعامل معها المستخدم.
/// يُستخدم في:
///   - تتبع المعاملات المالية (من دفعت له؟ من دفع لي؟)
///   - ربط الملاحظات بالأشخاص
///   - CRM بسيط للاجتماعات والتواصل
/// </summary>
public class Contact : BaseEntity
{
    /// <summary>الاسم الأول</summary>
    public required string FirstName { get; set; }

    /// <summary>اسم العائلة (اختياري)</summary>
    public string? LastName { get; set; }

    /// <summary>الاسم الكامل المحسوب - مفيد في الاستعلامات</summary>
    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}";

    /// <summary>البريد الإلكتروني (اختياري)</summary>
    public string? Email { get; set; }

    /// <summary>رقم الهاتف (اختياري)</summary>
    public string? Phone { get; set; }

    /// <summary>اسم الشركة أو المؤسسة (اختياري)</summary>
    public string? Company { get; set; }

    /// <summary>المسمى الوظيفي (اختياري)</summary>
    public string? JobTitle { get; set; }

    /// <summary>تصنيف طبيعة العلاقة مع جهة الاتصال</summary>
    public ContactType Type { get; set; } = ContactType.Other;

    /// <summary>ملاحظات عن جهة الاتصال: كيف تعارفنا، آخر تواصل...</summary>
    public string? Notes { get; set; }

    /// <summary>رابط صورة جهة الاتصال (اختياري)</summary>
    public string? AvatarUrl { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك لجهة الاتصال</summary>
    public Guid UserId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;

    /// <summary>المعاملات المالية المرتبطة بهذه الجهة</summary>
    public ICollection<Transaction> Transactions { get; set; } = [];

    /// <summary>الملاحظات المرتبطة بهذه الجهة</summary>
    public ICollection<Note> Notes2 { get; set; } = [];
}
