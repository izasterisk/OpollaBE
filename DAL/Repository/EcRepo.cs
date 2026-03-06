using DAL.Data;
using DAL.Data.Models;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class EcRepo : IEcRepo
{
    private readonly OpollaDbContext _context;

    public EcRepo(OpollaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ec>> GetByDateAsync(string date)
    {
        return await _context.Ecs
            .Where(e => e.Date == date)
            .ToListAsync();
    }

    public async Task BulkInsertAsync(List<Ec> records)
    {
        await _context.Ecs.AddRangeAsync(records);
        await _context.SaveChangesAsync();
    }
}
