using BLL.DTOs.Classes;

namespace BLL.Interfaces.Infrastructure;

public interface IClassApi
{
    Task<ClassPagingResponseDTO> GetClassListAsync(
        string token, 
        int centerId, 
        int page = 1, 
        int pageSize = 10, 
        string sortBy = "DESC", 
        bool isDesc = true, 
        bool showReport = true,
        CancellationToken cancellationToken = default);
}