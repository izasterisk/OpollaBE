using BLL.DTOs.Ec;

namespace BLL.Interfaces.IRepository;

public interface IEcRepo
{
    Task<List<EcDTO>> GetNameAndPercByDateAsync(string date);
    Task BulkCreateAsync(string date, List<EcDTO> records);
}
