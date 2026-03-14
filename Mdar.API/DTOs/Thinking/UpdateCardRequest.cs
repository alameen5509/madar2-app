using System.ComponentModel.DataAnnotations;
using Mdar.Core.Enums;

namespace Mdar.API.DTOs.Thinking;

/// <summary>
/// طلب التحديث الجزئي للبطاقة.
/// يُستخدم لكل من: تعديل المحتوى، وتحديث الموضع/الحجم بعد السحب.
/// الحقول null تعني "لا تغيير".
/// </summary>
public class UpdateCardRequest
{
    [StringLength(200, MinimumLength = 1)]
    public string? Title { get; set; }

    [StringLength(2000)]
    public string? Content { get; set; }

    public CardType? CardType { get; set; }

    public double? PositionX { get; set; }
    public double? PositionY { get; set; }

    [Range(80, 800)]
    public double? Width { get; set; }

    [Range(60, 600)]
    public double? Height { get; set; }

    public int? ZIndex { get; set; }
}
