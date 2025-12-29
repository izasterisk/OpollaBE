using BLL.DTOs.Students;
using BLL.Interfaces.Infrastructure;

namespace Infrastructure.Students;

public class StudentApi : IStudentApi
{
    private readonly ApiHelper _apiHelper;
    private readonly string _baseUrl;

    public StudentApi(ApiHelper apiHelper)
    {
        _apiHelper = apiHelper;
        _baseUrl = Environment.GetEnvironmentVariable("URL") 
            ?? throw new InvalidOperationException("URL not found in environment variables");
    }

    public async Task<StudentPagingResponseDTO> GetStudentListAsync(
        string token, 
        string centerId = "all",
        int page = 1, 
        int pageSize = 10, 
        string sortBy = "id", 
        string orderBy = "DESC", 
        bool isShowReport = true,
        string? classIds = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = $"page={page}&pageSize={pageSize}&sortBy={sortBy}&orderBy={orderBy}&isShowReport={isShowReport.ToString().ToLower()}";
        
        if (!string.IsNullOrEmpty(classIds))
        {
            queryParams += $"&classIds={classIds}";
        }
        
        var url = $"{_baseUrl.TrimEnd('/')}/management/student?{queryParams}";
        
        var headers = new Dictionary<string, string>
        {
            { "Center-Id", centerId }
        };

        return await _apiHelper.GetWithAuthAndHeadersAsync<StudentPagingResponseDTO>(url, token, headers, cancellationToken)
            ?? throw new Exception("Get student list failed: Invalid response from server");
    }
}