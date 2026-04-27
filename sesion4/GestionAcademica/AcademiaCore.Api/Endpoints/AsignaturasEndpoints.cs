// ── Api/Endpoints/AsignaturasEndpoints.cs ────────────────────────────
namespace AcademiaCore.Api.Endpoints;

using AcademiaCore.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

public static class AsignaturasEndpoints
{
    public static void MapAsignaturas(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/asignaturas")
                       .WithTags("Asignaturas")
                       .WithOpenApi();

        grupo.MapGet("/", ObtenerTodasAsync)
             .WithSummary("Obtiene todas las asignaturas activas");

        grupo.MapGet("/{id:int}", ObtenerPorIdAsync)
             .WithSummary("Obtiene una asignatura por su ID");

        grupo.MapGet("/disponibles", ObtenerDisponiblesAsync)
             .WithSummary("Obtiene asignaturas con plazas disponibles");
    }

    // ── GET /api/asignaturas ──────────────────────────────────────────
    private static async Task<IResult> ObtenerTodasAsync(
        [FromServices] IRepositorioAsignaturas repositorio,
        CancellationToken                      ct)
    {
        var asignaturas = await repositorio.ObtenerTodosAsync(ct);

        return Results.Ok(asignaturas.Select(AsignaturaResponse.Desde));
    }

    // ── GET /api/asignaturas/{id} ─────────────────────────────────────
    private static async Task<IResult> ObtenerPorIdAsync(
        int                                    id,
        [FromServices] IRepositorioAsignaturas repositorio,
        CancellationToken                      ct)
    {
        var asignatura = await repositorio.ObtenerAsync(id, ct);

        if (asignatura is null)
            return Results.NotFound(new { Error = $"Asignatura {id} no encontrada." });

        return Results.Ok(AsignaturaResponse.Desde(asignatura));
    }

    // ── GET /api/asignaturas/disponibles ─────────────────────────────
    private static async Task<IResult> ObtenerDisponiblesAsync(
        [FromServices] IRepositorioAsignaturas repositorio,
        CancellationToken                      ct)
    {
        var asignaturas = await repositorio.ObtenerTodosAsync(ct);

        var disponibles = asignaturas
            .Where(a => a.TienePlazas)
            .Select(AsignaturaResponse.Desde);

        return Results.Ok(disponibles);
    }
}

// ── DTO de respuesta ──────────────────────────────────────────────────
record AsignaturaResponse(
    int    Id,
    string Codigo,
    string Nombre,
    int    Creditos,
    int    PlazasMaximas,
    int    PlazasOcupadas,
    int    PlazasDisponibles,
    bool   TienePlazas)
{
    // Factory method estático para no repetir el mapeo
    public static AsignaturaResponse Desde(
        AcademiaCore.Domain.Entities.Asignatura a)
        => new(a.Id,
               a.Codigo,
               a.Nombre,
               a.Creditos,
               a.PlazasMaximas,
               a.PlazasOcupadas,
               a.PlazasDisponibles,
               a.TienePlazas);
}