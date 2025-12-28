using BLL.DTOs.Classes;
using BLL.Interfaces;
using BLL.Interfaces.Infrastructure;

namespace BLL.Services.Classes;

public class ClassService : IClassService
{
    private readonly IClassApi _classApi;
    
    public ClassService(IClassApi classApi)
    {
        _classApi = classApi;
    }

    public async Task<ClassPagingResponseDTO> GetAllClassesAsync(string token, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var rawClasses = await _classApi.GetClassListAsync(token, cancellationToken: cancellationToken);
        var allClasses = rawClasses.Data;
        
        if (rawClasses.TotalPages > 1)
        {
            for (var i = 2; i <= rawClasses.TotalPages; i++)
            {
                var responseClasses = await _classApi.GetClassListAsync(token, page: i, cancellationToken: cancellationToken);
                allClasses.AddRange(responseClasses.Data);
            }
        }
        
        var filteredClasses = allClasses.Where(c => c.HomeLearningReport != null).ToList();
        var totalFiltered = filteredClasses.Count;
        var totalPages = (int)Math.Ceiling((double)totalFiltered / pageSize);
        
        var paginatedClasses = filteredClasses
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return new ClassPagingResponseDTO
        {
            Data = paginatedClasses,
            Page = page,
            PageSize = pageSize,
            Total = totalFiltered,
            TotalPages = totalPages
        };
    }
}