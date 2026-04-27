// ── Api/Endpoints/EstudiantesEndpoints.cs ────────────────────────────
namespace AcademiaCore.Api.Endpoints;

using AcademiaCore.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

public static class EstudiantesEndpoints
{
    public static void MapEstudiantes(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/estudiantes")
                       .WithTags("Estudiantes")
                       .WithOpenApi();

        grupo.MapGet("/", ObtenerTodosAsync)
             .WithSummary("Obtiene todos los estudiantes activos");

        grupo.MapGet("/{id:int}", ObtenerPorIdAsync)
             .WithSummary("Obtiene un estudiante por su ID");
    }

    // ── GET /api/estudiantes ──────────────────────────────────────────
    private static async Task<IResult> ObtenerTodosAsync(
        [FromServices] IRepositorioEstudiantes repositorio,
        CancellationToken                      ct)
    {
        var estudiantes = await repositorio.ObtenerTodosAsync(ct);

        var response = estudiantes.Select(e => new EstudianteResponse(
            e.Id,
            e.NombreCompleto,
            e.Nombre,
            e.Apellidos,
            e.Email.Valor,
            e.Activo));

        return Results.Ok(response);
    }

    // ── GET /api/estudiantes/{id} ─────────────────────────────────────
    private static async Task<IResult> ObtenerPorIdAsync(
        int                                    id,
        [FromServices] IRepositorioEstudiantes repositorio,
        CancellationToken                      ct)
    {
        var estudiante = await repositorio.ObtenerAsync(id, ct);

        if (estudiante is null)
            return Results.NotFound(new { Error = $"Estudiante {id} no encontrado." });

        return Results.Ok(new EstudianteResponse(
            estudiante.Id,
            estudiante.NombreCompleto,
            estudiante.Nombre,
            estudiante.Apellidos,
            estudiante.Email.Valor,
            estudiante.Activo));
    }
}

// ── DTO de respuesta ──────────────────────────────────────────────────
record EstudianteResponse(
    int    Id,
    string NombreCompleto,
    string Nombre,
    string Apellidos,
    string Email,
    bool   Activo);