namespace GestionAcademica.Infrastructure.Adapters.Web;

using GestionAcademica.Application.DTOs;
using GestionAcademica.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Adaptador primario Web — controladores API o rutas.
/// Traduce HTTP → casos de uso → HTTP.
/// No contiene lógica de negocio.
/// </summary>
public static class AlumnosController
{
    public static void MapAlumnos(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/alumnos")
                       .WithTags("Alumnos")
                       .WithOpenApi();

        // GET /api/alumnos
        grupo.MapGet("/", async (
            [FromServices] ObtenerAlumnosHandler handler,
            CancellationToken ct) =>
            Results.Ok(await handler.EjecutarAsync(ct)))
         .WithSummary("Obtiene todos los alumnos activos");

        // GET /api/alumnos/{id}
        grupo.MapGet("/{id:int}", async (
            int                              id,
            [FromServices] ObtenerAlumnoPorIdHandler handler,
            CancellationToken ct) =>
        {
            var alumno = await handler.EjecutarAsync(id, ct);
            return alumno is null
                ? Results.NotFound(new { Error = $"Alumno {id} no encontrado." })
                : Results.Ok(alumno);
        })
        .WithSummary("Obtiene un alumno por su ID");

        // POST /api/alumnos
        grupo.MapPost("/", async (
            [FromBody]     CrearAlumnoRequest  request,
            [FromServices] CrearAlumnoHandler  handler,
            CancellationToken ct) =>
        {
            try
            {
                var dto = await handler.EjecutarAsync(request, ct);
                return Results.Created($"/api/alumnos/{dto.Id}", dto);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .WithSummary("Crea un nuevo alumno");

        // GET /api/alumnos/{id}/matriculas
        grupo.MapGet("/{id:int}/matriculas", async (
            int                                      id,
            [FromServices] ObtenerMatriculasAlumnoHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await handler.EjecutarAsync(id, ct));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Error = ex.Message });
            }
        })
        .WithSummary("Obtiene las matrículas de un alumno");
    }
}