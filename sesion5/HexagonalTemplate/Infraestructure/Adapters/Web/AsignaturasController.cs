namespace GestionAcademica.Infrastructure.Adapters.Web;

using GestionAcademica.Application.DTOs;
using GestionAcademica.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

// ── Controlador de Asignaturas ────────────────────────────────────────
public static class AsignaturasController
{
    public static void MapAsignaturas(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/asignaturas")
                       .WithTags("Asignaturas")
                       .WithOpenApi();

        // GET /api/asignaturas
        grupo.MapGet("/", async (
            [FromServices] ObtenerAsignaturasHandler handler,
            CancellationToken ct) =>
            Results.Ok(await handler.EjecutarAsync(ct)))
         .WithSummary("Obtiene todas las asignaturas activas");

        // PATCH /api/asignaturas/{id}/desactivar
        // Se usa PATCH (modificación parcial) en lugar de DELETE
        // porque la asignatura no se elimina, solo cambia su estado.
        grupo.MapPatch("/{id:int}/desactivar", async (
            int                                    id,
            [FromServices] DesactivarAsignaturaHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var dto = await handler.EjecutarAsync(id, ct);
                return Results.Ok(new
                {
                    Mensaje    = $"La asignatura '{dto.Nombre}' ha sido desactivada. " +
                                 "No se podrán crear nuevas matrículas para esta asignatura.",
                    Asignatura = dto
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // La asignatura ya estaba inactiva — regla de negocio del dominio
                return Results.Conflict(new { Error = ex.Message });
            }
        })
        .WithSummary("Desactiva una asignatura — las matrículas en ella quedan bloqueadas");
    
    }

    
}

// ── Controlador de Matrículas ─────────────────────────────────────────
public static class MatriculasController
{
    public static void MapMatriculas(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/matriculas")
                       .WithTags("Matrículas")
                       .WithOpenApi();

        // POST /api/matriculas
        grupo.MapPost("/", async (
            [FromBody]     MatricularAlumnoRequest request,
            [FromServices] MatricularAlumnoHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var dto = await handler.EjecutarAsync(request, ct);
                return Results.Created($"/api/matriculas/{dto.Id}", dto);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .WithSummary("Matricula a un alumno en una asignatura");
    }
}