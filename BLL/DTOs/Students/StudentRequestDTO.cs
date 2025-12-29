using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Students;

public class StudentRequestDTO
{
    [Required(ErrorMessage = "Token không được để trống.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "ClassId là bắt buộc.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "ClassId phải là một số (VD: 1715).")]
    public string ClassId { get; set; } = string.Empty;
}