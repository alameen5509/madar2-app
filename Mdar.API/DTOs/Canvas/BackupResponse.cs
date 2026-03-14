namespace Mdar.API.DTOs.Canvas;

/// <summary>
/// استجابة معلومات النسخة الاحتياطية.
/// لا تُعيد البيانات المشفرة — فقط البيانات الوصفية للعرض في القائمة.
/// لتنزيل الملف استخدم GET /api/canvas/backups/{id}/download
/// </summary>
public class BackupResponse
{
    public Guid Id { get; set; }

    /// <summary>اسم الملف الأصلي</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>حجم الملف بالبايت</summary>
    public long SizeBytes { get; set; }

    /// <summary>حجم الملف كنص مقروء (KB / MB)</summary>
    public string SizeFormatted => SizeBytes switch
    {
        < 1024               => $"{SizeBytes} B",
        < 1024 * 1024        => $"{SizeBytes / 1024.0:F1} KB",
        _                    => $"{SizeBytes / (1024.0 * 1024):F2} MB"
    };

    /// <summary>ملاحظة المستخدم الاختيارية</summary>
    public string? Label { get; set; }

    /// <summary>تاريخ رفع النسخة (UTC)</summary>
    public DateTime CreatedAt { get; set; }
}
