using BLL.DTOs.Students;
using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;

namespace BLL.Services;

public class StudentService : IStudentService
{
    private readonly IStudentApi _studentApi;
    
    public StudentService(IStudentApi studentApi)
    {
        _studentApi = studentApi;
    }

    public async Task<StudentPagingResponseDTO> GetAllStudentsAsync(StudentRequestDTO request,
        int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await _studentApi.GetStudentListAsync(
            request.Token, 
            page: page,
            pageSize: pageSize,
            classIds: request.ClassId,
            cancellationToken: cancellationToken);
    }
}
