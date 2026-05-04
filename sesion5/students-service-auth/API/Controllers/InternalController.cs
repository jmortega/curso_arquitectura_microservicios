using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Students.Application.UseCases;

namespace Students.API.Controllers;

/// <summary>
/// Endpoints internos para comunicación entre microservicios.
/// NO requieren autenticación JWT — solo accesibles dentro de la red Docker.
/// En producción real se protegerían con mutual TLS o API key de red interna.
/// </summary>
[ApiController]
[Route("api/internal/students")]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = "internal")]
public class InternalController : ControllerBase
{
    private readonly IStudentService _service;

    public InternalController(IStudentService service) => _service = service;

    /// <summary>
    /// Consulta un alumno por ID — usado por enrollments-service para verificar
    /// que el alumno existe y está activo antes de matricularlo.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var student = await _service.GetByIdAsync(id);
        return student is null
            ? NotFound()
            : Ok(student);
    }
}
