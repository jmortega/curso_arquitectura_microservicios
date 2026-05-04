namespace GestionAcademica.Infrastructure.Adapters.Persistence;

using GestionAcademica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Adaptador de persistencia — implementación SQL con EF Core.
/// Vive en Infrastructure, nunca en el dominio.
/// </summary>
public class AcademiaDbContext : DbContext
{
    public AcademiaDbContext(DbContextOptions<AcademiaDbContext> options)
        : base(options) { }

    public DbSet<Alumno>     Alumnos     => Set<Alumno>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();
    public DbSet<Matricula>  Matriculas  => Set<Matricula>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Alumno ────────────────────────────────────────────────────
        modelBuilder.Entity<Alumno>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            e.Property(x => x.Apellidos).IsRequired().HasMaxLength(150);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.Property(x => x.Activo).HasDefaultValue(true);
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ── Asignatura ────────────────────────────────────────────────
        modelBuilder.Entity<Asignatura>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).IsRequired().HasMaxLength(20);
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
            e.Property(x => x.Creditos).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        // ── Matrícula ─────────────────────────────────────────────────
        modelBuilder.Entity<Matricula>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Periodo).IsRequired().HasMaxLength(10);
            e.Property(x => x.FechaAlta).IsRequired();
            e.Property(x => x.Activa).HasDefaultValue(true);

            e.HasOne(x => x.Alumno)
             .WithMany()
             .HasForeignKey(x => x.AlumnoId);

            e.HasOne(x => x.Asignatura)
             .WithMany()
             .HasForeignKey(x => x.AsignaturaId);
        });

        // ── Datos de ejemplo ──────────────────────────────────────────
        modelBuilder.Entity<Alumno>().HasData(
            new { Id = 1, Nombre = "Ana",    Apellidos = "García López",    Email = "ana.garcia@academia.es",    Activo = true },
            new { Id = 2, Nombre = "Carlos", Apellidos = "Martínez Ruiz",   Email = "carlos.martinez@academia.es", Activo = true },
            new { Id = 3, Nombre = "Laura",  Apellidos = "Sánchez Pérez",   Email = "laura.sanchez@academia.es",   Activo = true }
        );

        modelBuilder.Entity<Asignatura>().HasData(
            new { Id = 1, Codigo = "CS101", Nombre = "Programación I",      Creditos = 6, Activa = true },
            new { Id = 2, Codigo = "CS102", Nombre = "Bases de Datos",      Creditos = 6, Activa = true },
            new { Id = 3, Codigo = "CS103", Nombre = "Redes",               Creditos = 6, Activa = true },
            new { Id = 4, Codigo = "CS104", Nombre = "Sistemas Operativos", Creditos = 6, Activa = true }
        );
    }
}