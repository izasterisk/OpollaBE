using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Students;

public class HomeLearningRequestDTO
{
    [Required(ErrorMessage = "Token không được để trống.")]
    public string Token { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "ClassId là bắt buộc.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "ClassId phải là một số (VD: 1715).")]
    public string ClassId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn ngày.")]
    public DateOnly ChoosenDate { get; set; }
}