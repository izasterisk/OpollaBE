using DAL.Data.Models;

namespace DAL.IRepository;

public interface IEcRepo
{
    Task<List<Ec>> GetByDateAsync(string date);
    Task BulkInsertAsync(List<Ec> records);
}
