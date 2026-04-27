// ── Api/Endpoints/MatriculacionEndpoints.cs ───────────────────────────
namespace AcademiaCore.Api.Endpoints;

using AcademiaCore.Application;
using AcademiaCore.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

public static class MatriculacionEndpoints
{
    public static void MapMatriculacion(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/matriculas")
                       .WithTags("Matriculación")
                       .WithOpenApi();

        grupo.MapPost("/", MatricularAsync)
             .WithSummary("Matricula a un estudiante en un conjunto de asignaturas");

        // Motivo llega como query parameter: DELETE /api/matriculas/{id}?motivo=...
        grupo.MapDelete("/{id:guid}", CancelarAsync)
             .WithSummary("Cancela una matrícula activa");

        grupo.MapGet("/estudiante/{estudianteId:int}", ObtenerPorEstudianteAsync)
             .WithSummary("Obtiene las matrículas de un estudiante");
    }

    // ── POST /api/matriculas ──────────────────────────────────────────
    private static async Task<IResult> MatricularAsync(
        [FromBody]     MatricularRequest     request,
        [FromServices] ServicioMatriculacion servicio,
        CancellationToken                    ct)
    {
        try
        {
            var matricula = await servicio.MatricularAsync(
                request.EstudianteId,
                request.Anyo,
                request.Semestre,
                request.IdsAsignaturas,
                ct);

            return Results.Created(
                $"/api/matriculas/{matricula.Id}",
                new MatriculaResponse(
                    matricula.Id,
                    matricula.EstudianteId,
                    matricula.Periodo.Codigo,
                    matricula.TotalCreditos,
                    matricula.Estado.ToString(),
                    matricula.Lineas
                             .Select(l => new LineaResponse(l.NombreAsignatura, l.Creditos))
                             .ToList()));
        }
        catch (MatriculacionException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { Error = ex.Message });
        }
    }

    // ── DELETE /api/matriculas/{id}?motivo=Baja+voluntaria ───────────
    private static async Task<IResult> CancelarAsync(
        Guid                               id,
        [FromQuery]    string              motivo,
        [FromServices] ServicioMatriculacion servicio,
        CancellationToken                  ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            return Results.BadRequest(new { Error = "El motivo es obligatorio." });

        try
        {
            await servicio.CancelarAsync(id, motivo, ct);
            return Results.NoContent();
        }
        catch (MatriculacionException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { Error = ex.Message });
        }
    }

    // ── GET /api/matriculas/estudiante/{estudianteId} ─────────────────
    private static async Task<IResult> ObtenerPorEstudianteAsync(
        int                                estudianteId,
        [FromServices] ServicioMatriculacion servicio,
        CancellationToken                   ct)
    {
        try
        {
            var matriculas = await servicio
                .ObtenerPorEstudianteAsync(estudianteId, ct);

            var response = matriculas.Select(m => new MatriculaResponse(
                m.Id,
                m.EstudianteId,
                m.Periodo.Codigo,
                m.TotalCreditos,
                m.Estado.ToString(),
                m.Lineas
                 .Select(l => new LineaResponse(l.NombreAsignatura, l.Creditos))
                 .ToList()));

            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { Error = ex.Message });
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────
record MatricularRequest(
    int       EstudianteId,
    int       Anyo,
    int       Semestre,
    List<int> IdsAsignaturas);

record MatriculaResponse(
    Guid                MatriculaId,
    int                 EstudianteId,
    string              PeriodoCodigo,
    int                 TotalCreditos,
    string              Estado,
    List<LineaResponse> Lineas);

record LineaResponse(string Asignatura, int Creditos);