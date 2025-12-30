using BLL.DTOs.Students;

namespace BLL.Interfaces;

public interface IStudentService
{
    Task<StudentPagingResponseDTO> GetAllStudentsAsync(
        StudentRequestDTO request,
        int page = 1, 
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    
    Task<HomeLearningPagingResponseDTO> GetStudentsProgressByClassAsync(
        HomeLearningRequestDTO dto, 
        int page = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default);
}

