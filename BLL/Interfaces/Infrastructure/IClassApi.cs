using BLL.DTOs.Classes;

namespace BLL.Interfaces.Infrastructure;

public interface IClassApi
{
    Task<ClassPagingResponseDTO> GetClassListAsync(
        string token, 
        string centerId = "all",
        int page = 1, 
        int pageSize = 10, 
        string sortBy = "id", 
        string orderBy = "DESC", 
        bool isShowReport = true,
        CancellationToken cancellationToken = default);
}