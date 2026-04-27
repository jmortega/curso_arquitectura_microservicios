using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Students.Application.DTOs;
using Students.Application.UseCases;

namespace Students.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]   // todos los endpoints requieren token JWT válido
public class StudentsController : ControllerBase
{
    private readonly IStudentService _service;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentService service, ILogger<StudentsController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Obtiene todos los alumnos. Requiere rol: Admin, Teacher o ReadOnly.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Teacher,ReadOnly")]
    [ProducesResponseType(typeof(IEnumerable<StudentDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        => Ok(await _service.GetAllAsync(onlyActive));

    /// <summary>Obtiene un alumno por su ID. Requiere rol: Admin, Teacher o ReadOnly.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher,ReadOnly")]
    [ProducesResponseType(typeof(StudentDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var student = await _service.GetByIdAsync(id);
        return student is null
            ? NotFound(new { message = $"Alumno {id} no encontrado." })
            : Ok(student);
    }

    /// <summary>Registra un nuevo alumno. Requiere rol: Admin o Teacher.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(typeof(StudentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
    {
        try
        {
            var student = await _service.CreateAsync(request);
            _logger.LogInformation("Alumno creado: {Id} por {User}",
                student.Id, User.Identity?.Name);
            return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflicto al crear alumno: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Actualiza los datos de un alumno. Requiere rol: Admin o Teacher.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(typeof(StudentDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest request)
    {
        try
        {
            var student = await _service.UpdateAsync(id, request);
            return student is null
                ? NotFound(new { message = $"Alumno {id} no encontrado." })
                : Ok(student);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Elimina (soft delete) un alumno. Requiere rol: Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted
            ? NoContent()
            : NotFound(new { message = $"Alumno {id} no encontrado." });
    }
}
