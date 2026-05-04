using GestionAcademica.Application.UseCases;
using GestionAcademica.Domain.Ports;
using GestionAcademica.Domain.Services;
using GestionAcademica.Infrastructure.Adapters.ExternalServices;
using GestionAcademica.Infrastructure.Adapters.Persistence;
using GestionAcademica.Infrastructure.Adapters.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Swagger ───────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Gestión Académica API",
        Version     = "v1",
        Description = """
            Arquitectura Hexagonal (Ports & Adapters)

            A. Capa de Dominio  — Entities, ValueObjects, Services, Ports
            B. Capa de Aplicación — UseCases, DTOs
            C. Capa de Infraestructura — Adapters/Persistence, Adapters/ExternalServices, Adapters/Web
            """
    });
});

// ── C. Infraestructura: Adaptador de Persistencia (SQL) ───────────────
builder.Services.AddDbContext<AcademiaDbContext>(options =>
    options.UseSqlite("Data Source=academia.db"));

// Puertos → Adaptadores (Inversión de Dependencias)
builder.Services.AddScoped<IAlumnoRepository,    AlumnoRepository>();
builder.Services.AddScoped<IAsignaturaRepository, AsignaturaRepository>();
builder.Services.AddScoped<IMatriculaRepository,  MatriculaRepository>();

// ── C. Infraestructura: Adaptador de Servicio Externo ─────────────────
builder.Services.AddScoped<INotificacionService, NotificacionEmailService>();

// ── A. Dominio: Domain Service ────────────────────────────────────────
builder.Services.AddScoped<ServicioMatriculacion>();

// ── B. Aplicación: Casos de Uso (UseCases) ────────────────────────────
builder.Services.AddScoped<ObtenerAlumnosHandler>();
builder.Services.AddScoped<ObtenerAlumnoPorIdHandler>();
builder.Services.AddScoped<CrearAlumnoHandler>();
builder.Services.AddScoped<ObtenerMatriculasAlumnoHandler>();
builder.Services.AddScoped<MatricularAlumnoHandler>();
builder.Services.AddScoped<ObtenerAsignaturasHandler>();
builder.Services.AddScoped<DesactivarAsignaturaHandler>();

// ── Build ─────────────────────────────────────────────────────────────
var app = builder.Build();

// Inicializar base de datos con datos de ejemplo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AcademiaDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ── Swagger UI ────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestión Académica v1");
    c.RoutePrefix    = string.Empty;
    c.DocumentTitle  = "Gestión Académica — Arquitectura Hexagonal";
});

// ── C. Infraestructura: Adaptadores Web (rutas) ───────────────────────
app.MapAlumnos();
app.MapAsignaturas();
app.MapMatriculas();

// Endpoint informativo sobre la arquitectura
app.MapGet("/arquitectura", () => Results.Ok(new
{
    Patron = "Arquitectura Hexagonal (Ports & Adapters)",
    Capas  = new
    {
        A_Dominio = new
        {
            Descripcion = "El núcleo. Sin dependencias externas.",
            Carpetas    = new[] {
                "Domain/Entities/     → Alumno.cs, Asignatura.cs, Matricula.cs",
                "Domain/ValueObjects/ → Direccion.cs, Periodo.cs",
                "Domain/Services/     → ServicioMatriculacion.cs",
                "Domain/Ports/        → IAlumnoRepository.cs, IMatriculaRepository.cs"
            }
        },
        B_Aplicacion = new
        {
            Descripcion = "Orquesta el flujo de datos desde y hacia el dominio.",
            Carpetas    = new[] {
                "Application/UseCases/ → MatricularAlumnoHandler.cs, ObtenerAlumnosHandler.cs",
                "Application/DTOs/     → AlumnoDto.cs, MatriculaDto.cs"
            }
        },
        C_Infraestructura = new
        {
            Descripcion = "Implementaciones de interfaces y adaptadores.",
            Carpetas    = new[] {
                "Infrastructure/Adapters/Persistence/       → AlumnoRepository.cs (SQL/EF Core)",
                "Infrastructure/Adapters/ExternalServices/  → NotificacionEmailService.cs",
                "Infrastructure/Adapters/Web/               → AlumnosController.cs (rutas)"
            }
        }
    }
}))
.WithTags("Arquitectura")
.WithSummary("Descripción de la arquitectura del proyecto")
.ExcludeFromDescription();

app.Run();