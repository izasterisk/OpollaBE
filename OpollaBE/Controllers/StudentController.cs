using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using BLL.DTOs;
using BLL.DTOs.Students;

namespace OpollaBE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentController : BaseController
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IStudentService studentService, ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<APIResponse>> GetStudents(
        [FromBody] StudentRequestDTO request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var students = await _studentService.GetAllStudentsAsync(request, page, pageSize, cancellationToken);
            return SuccessResponse(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStudents: {Message}", ex.Message);
            return HandleException(ex);
        }
    }

    [HttpPost("progress")]
    public async Task<ActionResult<APIResponse>> GetStudentsProgress(
        [FromBody] HomeLearningRequestDTO request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var progress = await _studentService.GetStudentsProgressByClassAsync(request, page, pageSize, cancellationToken);
            return SuccessResponse(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStudentsProgress: {Message}", ex.Message);
            return HandleException(ex);
        }
    }
}
