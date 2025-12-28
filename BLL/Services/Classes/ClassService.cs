using BLL.DTOs.Classes;
using BLL.Interfaces.Infrastructure;

namespace BLL.Services.Classes;

public class ClassService
{
    private readonly IClassApi _classApi;
    
    public ClassService(IClassApi classApi)
    {
        _classApi = classApi;
    }

    public async Task<ClassPagingResponseDTO> GetAllClassesAsync(string token, CancellationToken cancellationToken = default)
    {
        
    }
}