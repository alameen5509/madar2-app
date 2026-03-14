using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Mdar.API.DTOs.Canvas;

/// <summary>
/// طلب رفع نسخة احتياطية مشفرة إلى السحابة.
/// يُستخدم كـ multipart/form-data لأنه يحتوي على ملف ثنائي.
/// </summary>
public class UploadBackupRequest
{
    /// <summary>ملف .mdar المشفر (إلزامي)</summary>
    [Required(ErrorMessage = "الملف مطلوب")]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// ملاحظة اختيارية للتعرف على النسخة
    /// (مثال: "قبل إعادة الهيكلة الكبيرة")
    /// </summary>
    [MaxLength(100, ErrorMessage = "الملاحظة لا تتجاوز 100 حرف")]
    public string? Label { get; set; }
}
