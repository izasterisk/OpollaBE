using BLL.DTOs.Students;
using BLL.Helper;
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

    public async Task<HomeLearningPagingResponseDTO> GetStudentsProgressByClassAsync(
        HomeLearningRequestDTO dto, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (dto.ChoosenDate > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)))
            throw new InvalidCastException("Cannot choose date in the future.");
        
        var students = await _studentApi.GetStudentListAsync(
            dto.Token, 
            page: 1,
            pageSize: 100,
            classIds: dto.ClassId,
            cancellationToken: cancellationToken);

        var learningProgress = new List<HomeLearningDTO>();
        if (students.Data.Count > 0)
        {
            foreach (var student in students.Data)
            {
                var raw = await _studentApi.GetStudentHomeLearningAsync
                    (dto.Token, student.Id, pageSize: 50, classIds: dto.ClassId, 
                        cancellationToken: cancellationToken);
                if (raw.Data.Count > 0)
                {
                    var firstAssignDate = DateOnly.FromDateTime(raw.Data.First().AssignDate);
                    var lastAssignDate = DateOnly.FromDateTime(raw.Data.Last().AssignDate);
                    if (dto.ChoosenDate >= lastAssignDate && dto.ChoosenDate <= firstAssignDate)
                    {
                        var matchingRecord = raw.Data.FirstOrDefault(x => 
                            DateOnly.FromDateTime(x.AssignDate) == dto.ChoosenDate);
                        if (matchingRecord != null)
                        {
                            matchingRecord.StudentName = student.Name;
                            learningProgress.Add(matchingRecord);
                        }
                            
                    }
                    else
                    {
                        // Tính toán trang chứa bản ghi cần tìm
                        var targetPage = DateHelper.CalculatePageForDate(
                            dto.ChoosenDate, 
                            raw.PageSize, 
                            firstAssignDate, 
                            lastAssignDate);
                        
                        // Fetch dữ liệu từ trang đã tính toán
                        var targetPageData = await _studentApi.GetStudentHomeLearningAsync(
                            dto.Token, 
                            student.Id, 
                            page: targetPage,
                            pageSize: raw.PageSize, 
                            classIds: dto.ClassId, 
                            cancellationToken: cancellationToken);
                        
                        // Tìm bản ghi khớp trong trang mới
                        var matchingRecord = targetPageData.Data.FirstOrDefault(x => 
                            DateOnly.FromDateTime(x.AssignDate) == dto.ChoosenDate);
                        if (matchingRecord != null)
                        {
                            matchingRecord.StudentName = student.Name;
                            learningProgress.Add(matchingRecord);
                        }
                    }
                }
            }
        }

        // Tính toán paging
        var totalRecords = learningProgress.Count;
        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        
        var paginatedData = learningProgress
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return new HomeLearningPagingResponseDTO
        {
            Data = paginatedData,
            Page = page,
            PageSize = pageSize,
            Total = totalRecords,
            TotalPages = totalPages
        };
    }
}
