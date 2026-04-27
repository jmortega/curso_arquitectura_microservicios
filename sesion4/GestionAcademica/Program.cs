// ── Api/Program.cs ────────────────────────────────────────────────────
using AcademiaCore.Api.Endpoints;
using AcademiaCore.Application;
using AcademiaCore.Domain.Repositories;
using AcademiaCore.Infrastructure;
using AcademiaCore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new()
    {
        Title   = "AcademiaCore API",
        Version = "v1"
    }));

// Base de datos — SQLite para desarrollo, fácil de sustituir
builder.Services.AddDbContext<AcademiaDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=academia.db"));

// Servicios de aplicación
builder.Services.AddScoped<ServicioMatriculacion>();
builder.Services.AddScoped<IServicioEventos, ServicioEventosLog>();

// Repositorios (infraestructura)
builder.Services.AddScoped<IRepositorioMatriculas,  RepositorioMatriculas>();
builder.Services.AddScoped<IRepositorioEstudiantes, RepositorioEstudiantes>();
builder.Services.AddScoped<IRepositorioAsignaturas, RepositorioAsignaturas>();

var app = builder.Build();

// Crear la base de datos automáticamente en desarrollo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AcademiaDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapMatriculacion();
app.MapEstudiantes();     // ← nuevo
app.MapAsignaturas();

app.Run();