using ChessCampRegistration.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ChessCampRegistration.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.ToTable("Registrations");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.KidFullName).HasMaxLength(200).IsRequired();
            entity.Property(r => r.KidSchool).HasMaxLength(200).IsRequired();
            entity.Property(r => r.KidChessLevel).HasMaxLength(50).IsRequired();
            entity.Property(r => r.ParentName).HasMaxLength(200).IsRequired();
            entity.Property(r => r.ParentPhone).HasMaxLength(30).IsRequired();
            entity.Property(r => r.ParentEmail).HasMaxLength(200).IsRequired();
            entity.HasIndex(r => r.CreatedAt);
        });
    }
}
