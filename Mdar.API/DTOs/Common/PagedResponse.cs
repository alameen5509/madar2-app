namespace Mdar.API.DTOs.Common;

/// <summary>
/// غلاف موحَّد لجميع الاستجابات المقسَّمة إلى صفحات.
/// يُعيد بيانات الصفحة الحالية مع معلومات التنقل.
/// </summary>
/// <typeparam name="T">نوع عناصر القائمة</typeparam>
public sealed record PagedResponse<T>
{
    /// <summary>عناصر الصفحة الحالية</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>رقم الصفحة الحالية (يبدأ من 1)</summary>
    public int Page { get; init; }

    /// <summary>عدد العناصر في الصفحة الواحدة</summary>
    public int PageSize { get; init; }

    /// <summary>إجمالي عدد العناصر في جميع الصفحات</summary>
    public int TotalCount { get; init; }

    /// <summary>إجمالي عدد الصفحات</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>هل توجد صفحة تالية؟</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>هل توجد صفحة سابقة؟</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>إنشاء استجابة صفحات من قائمة ومعاملات الترحيل</summary>
    public static PagedResponse<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}
