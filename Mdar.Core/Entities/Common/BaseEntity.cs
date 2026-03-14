namespace Mdar.Core.Entities.Common;

/// <summary>
/// الكيان الأساسي المشترك الذي يرثه جميع الكيانات في النظام.
/// يوفر:
///   - معرّف فريد (Guid) لتجنب تعارضات المفاتيح الرقمية.
///   - تتبع تلقائي لتواريخ الإنشاء والتعديل.
///   - حذف ناعم (Soft Delete) للحفاظ على سجل البيانات.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// المعرّف الفريد للكيان (Primary Key).
    /// يُولَّد تلقائياً عند الإنشاء.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// تاريخ ووقت إنشاء السجل (UTC).
    /// يُضبط تلقائياً عند الإضافة ولا يتغير بعد ذلك.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ ووقت آخر تعديل على السجل (UTC).
    /// يُحدَّث تلقائياً في كل عملية حفظ.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// علامة الحذف الناعم.
    /// true = السجل محذوف منطقياً ولن يظهر في الاستعلامات العادية.
    /// false = السجل نشط ومرئي (القيمة الافتراضية).
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
