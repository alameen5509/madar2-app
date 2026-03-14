using System.ComponentModel.DataAnnotations;
using Mdar.Core.Enums;

namespace Mdar.API.DTOs.Thinking;

public class CreateCardRequest
{
    [Required(ErrorMessage = "عنوان البطاقة مطلوب")]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = "فكرة جديدة";

    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public CardType CardType { get; set; } = CardType.Note;

    /// <summary>الموضع الأفقي على اللوحة (بالبكسل)</summary>
    public double PositionX { get; set; } = 100;

    /// <summary>الموضع الرأسي على اللوحة (بالبكسل)</summary>
    public double PositionY { get; set; } = 100;

    [Range(80, 800)]
    public double Width { get; set; } = 220;

    [Range(60, 600)]
    public double Height { get; set; } = 160;
}
