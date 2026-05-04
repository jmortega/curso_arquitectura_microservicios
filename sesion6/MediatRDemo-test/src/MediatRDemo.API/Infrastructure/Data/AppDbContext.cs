using MediatRDemo.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediatRDemo.API.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnType("char(36)");
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(150);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);
            e.Property(u => u.IsActive).HasDefaultValue(true);
            e.Property(u => u.CreatedAt).HasColumnType("datetime(6)");
            e.Property(u => u.UpdatedAt).HasColumnType("datetime(6)");
        });

        // Datos semilla
        mb.Entity<User>().HasData(
            new { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                  Name = "Admin User",   Email = "admin@demo.com",
                  Role = "Admin", IsActive = true,
                  CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                  UpdatedAt = (DateTime?)null },
            new { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"),
                  Name = "Regular User", Email = "user@demo.com",
                  Role = "User",  IsActive = true,
                  CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                  UpdatedAt = (DateTime?)null },
            new { Id = Guid.Parse("33333333-0000-0000-0000-000000000003"),
                  Name = "Read Only",    Email = "readonly@demo.com",
                  Role = "ReadOnly", IsActive = false,
                  CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                  UpdatedAt = (DateTime?)null }
        );
    }
}
