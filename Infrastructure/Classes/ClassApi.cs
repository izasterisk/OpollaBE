using BLL.DTOs.Classes;
using BLL.Interfaces.Infrastructure;

namespace Infrastructure.Classes;

public class ClassApi : IClassApi
{
    private readonly ApiHelper _apiHelper;
    private readonly string _baseUrl;

    public ClassApi(ApiHelper apiHelper)
    {
        _apiHelper = apiHelper;
        _baseUrl = Environment.GetEnvironmentVariable("URL") 
            ?? throw new InvalidOperationException("URL not found in environment variables");
    }

    public async Task<ClassPagingResponseDTO> GetClassListAsync(
        string token, 
        int centerId, 
        int page = 1, 
        int pageSize = 10, 
        string sortBy = "DESC", 
        bool isDesc = true, 
        bool showReport = true,
        CancellationToken cancellationToken = default)
    {
        var queryParams = $"page={page}&pageSize={pageSize}&sortBy={sortBy}&isDesc={isDesc}&showReport={showReport}";
        var url = $"{_baseUrl.TrimEnd('/')}/api/management/class?{queryParams}";
        
        var headers = new Dictionary<string, string>
        {
            { "Center-Id", centerId.ToString() }
        };

        return await _apiHelper.GetWithAuthAndHeadersAsync<ClassPagingResponseDTO>(url, token, headers, cancellationToken)
            ?? throw new Exception("Get class list failed: Invalid response from server");
    }
}