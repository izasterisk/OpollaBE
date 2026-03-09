using AutoMapper;
using BLL.DTOs.Ec;
using BLL.Interfaces.IRepository;
using DAL.Data;
using DAL.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class EcRepo : IEcRepo
{
    private readonly OpollaDbContext _context;
    private readonly IMapper _mapper;

    public EcRepo(OpollaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EcDTO>> GetNameAndPercByDateAsync(string date)
    {
        return await _context.Ecs
            .Where(e => e.Date == date)
            .Select(e => new EcDTO { Name = e.Name, AvgPercent = e.AvgPercent })
            .ToListAsync();
    }

    public async Task BulkCreateAsync(string date, List<EcDTO> records)
    {
        var existing = await _context.Ecs
            .Where(e => e.Date == date)
            .ToListAsync();

        if (existing.Count > 0)
            _context.Ecs.RemoveRange(existing);

        var entities = _mapper.Map<List<Ec>>(records);
        await _context.Ecs.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }
}
