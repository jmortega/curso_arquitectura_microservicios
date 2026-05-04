using Enrollments.Application.Commands.CancelEnrollment;
using Enrollments.Application.Commands.CreateSubject;
using Enrollments.Application.Commands.DeleteSubject;
using Enrollments.Application.Commands.EnrollStudent;
using Enrollments.Application.Commands.UpdateSubject;
using Enrollments.Application.DTOs;
using Enrollments.Application.Queries.GetAllSubjects;
using Enrollments.Application.Queries.GetSubjectById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Enrollments.API.Controllers;

/// <summary>
/// Controlador DELGADO — solo dispatching via MediatR.
/// Toda la lógica reside en los Handlers (patrón Mediator).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SubjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SubjectsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Lista todas las asignaturas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubjectDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        => Ok(await _mediator.Send(new GetAllSubjectsQuery(onlyActive)));

    /// <summary>Obtiene una asignatura por ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
        => Ok(await _mediator.Send(new GetSubjectByIdQuery(id)));

    /// <summary>Crea una nueva asignatura.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubjectDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest req)
    {
        var result = await _mediator.Send(new CreateSubjectCommand(
            req.Code, req.Name, req.Description, req.Credits, req.MaxCapacity));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Actualiza una asignatura existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubjectDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSubjectRequest req)
        => Ok(await _mediator.Send(new UpdateSubjectCommand(
            id, req.Name, req.Description, req.Credits, req.MaxCapacity)));

    /// <summary>Elimina una asignatura.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteSubjectCommand(id));
        return NoContent();
    }
}
