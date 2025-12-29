using BLL.DTOs.Students;

namespace BLL.Interfaces.Infrastructure;

public interface IStudentApi
{
    Task<StudentPagingResponseDTO> GetStudentListAsync(
        string token,
        string centerId = "all",
        int page = 1,
        int pageSize = 10,
        string sortBy = "id",
        string orderBy = "DESC",
        bool isShowReport = true,
        string? classIds = null,
        CancellationToken cancellationToken = default);
}
