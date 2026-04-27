// ── Infrastructure/AcademiaDbContext.cs ──────────────────────────────
namespace AcademiaCore.Infrastructure;

using AcademiaCore.Domain.Aggregates;
using AcademiaCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AcademiaDbContext : DbContext
{
    public AcademiaDbContext(DbContextOptions<AcademiaDbContext> options)
        : base(options) { }

    public DbSet<Matricula>  Matriculas  => Set<Matricula>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Estudiante ────────────────────────────────────────────────
        modelBuilder.Entity<Estudiante>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Nombre)
             .IsRequired()
             .HasMaxLength(100);

            e.Property(x => x.Apellidos)
             .IsRequired()
             .HasMaxLength(150);

            e.Property(x => x.Activo)
             .IsRequired()
             .HasDefaultValue(true);

            // Email como Value Object owned
            e.OwnsOne(x => x.Email, email =>
            {
                email.Property(v => v.Valor)
                     .HasColumnName("Email")
                     .IsRequired()
                     .HasMaxLength(200);
            });
        });

        // ── Asignatura ────────────────────────────────────────────────
        modelBuilder.Entity<Asignatura>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Codigo)
             .IsRequired()
             .HasMaxLength(20);

            e.Property(x => x.Nombre)
             .IsRequired()
             .HasMaxLength(200);

            e.Property(x => x.Creditos).IsRequired();
            e.Property(x => x.PlazasMaximas).IsRequired();
            e.Property(x => x.PlazasOcupadas).IsRequired();
            e.Property(x => x.Activa).IsRequired();

            e.HasIndex(x => x.Codigo).IsUnique();
        });

        // ── Matricula ─────────────────────────────────────────────────
        modelBuilder.Entity<Matricula>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.EstudianteId).IsRequired();
            e.Property(x => x.Estado).IsRequired();
            e.Property(x => x.FechaCreacion).IsRequired();
            e.Property(x => x.FechaCancelacion);
            e.Property(x => x.MotivoCancelacion).HasMaxLength(500);

            e.OwnsOne(x => x.Periodo, periodo =>
            {
                periodo.Property(v => v.Anyo)
                       .HasColumnName("PeriodoAnyo")
                       .IsRequired();

                periodo.Property(v => v.Semestre)
                       .HasColumnName("PeriodoSemestre")
                       .IsRequired();
            });

            e.OwnsMany(x => x.Lineas, linea =>
            {
    linea.WithOwner().HasForeignKey("MatriculaId");

    // Clave primaria con valor autogenerado por la base de datos
    linea.Property<int>("Id")
         .ValueGeneratedOnAdd();

    linea.HasKey("Id");

    linea.Property(l => l.AsignaturaId).IsRequired();
    linea.Property(l => l.NombreAsignatura)
         .IsRequired()
         .HasMaxLength(200);
    linea.Property(l => l.Creditos).IsRequired();
});

            e.Ignore(x => x.EventosDominio);
        });

        // ── Seed data ─────────────────────────────────────────────────
        // Asignaturas — sin Value Objects, el seed es directo
        modelBuilder.Entity<Asignatura>().HasData(
            new { Id = 1, Codigo = "CS001", Nombre = "Programación I",
                  Creditos = 6, PlazasMaximas = 30, PlazasOcupadas = 0,
                  Activa = true },
            new { Id = 2, Codigo = "CS002", Nombre = "Bases de Datos",
                  Creditos = 6, PlazasMaximas = 30, PlazasOcupadas = 0,
                  Activa = true },
            new { Id = 3, Codigo = "CS003", Nombre = "Redes",
                  Creditos = 6, PlazasMaximas = 30, PlazasOcupadas = 0,
                  Activa = true }
        );

        // Estudiante — el owned type Email se seedea por separado
        // usando un tipo anónimo que coincide con las columnas reales
        modelBuilder.Entity<Estudiante>().HasData(
            new { Id = 1, Nombre = "Ana", Apellidos = "García López",
                  Activo = true }
        );

        // El valor del owned type se seedea a través de OwnsOne
        // referenciando la entidad padre por su Id
        modelBuilder.Entity<Estudiante>()
            .OwnsOne(e => e.Email)
            .HasData(new { EstudianteId = 1, Valor = "ana.garcia@universidad.es" });
    }
}