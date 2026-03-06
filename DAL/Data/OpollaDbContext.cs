using DAL.Data.Configuration;
using DAL.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data;

public class OpollaDbContext : DbContext
{
    public OpollaDbContext(DbContextOptions<OpollaDbContext> options) : base(options)
    {
    }

    public DbSet<Ec> Ecs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new EcConfiguration());
    }
}
