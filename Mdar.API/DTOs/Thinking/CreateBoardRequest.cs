using System.ComponentModel.DataAnnotations;

namespace Mdar.API.DTOs.Thinking;

public class CreateBoardRequest
{
    [Required(ErrorMessage = "عنوان اللوحة مطلوب")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "العنوان بين 1 و150 حرف")]
    public string Title { get; set; } = "لوحتي";

    [StringLength(500)]
    public string? Description { get; set; }
}
