using BLL.DTOs.Classes;

namespace BLL.Interfaces;

public interface IClassService
{
    Task<ClassPagingResponseDTO> GetAllClassesAsync(string token, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}
