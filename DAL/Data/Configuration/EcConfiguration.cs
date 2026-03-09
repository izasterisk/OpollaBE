using DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configuration;

public class EcConfiguration : IEntityTypeConfiguration<Ec>
{
    public void Configure(EntityTypeBuilder<Ec> builder)
    {
        builder.ToTable("Ec");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Date)
            .HasColumnName("Date")
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Date)
            .HasDatabaseName("IX_Ec_Date");

        builder.Property(e => e.AvgPercent)
            .HasColumnName("AvgPercent")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
    }
}
