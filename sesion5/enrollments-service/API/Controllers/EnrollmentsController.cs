using Enrollments.Application.Commands.CancelEnrollment;
using Enrollments.Application.Commands.EnrollStudent;
using Enrollments.Application.DTOs;
using Enrollments.Application.Queries.GetAllEnrollments;
using Enrollments.Application.Queries.GetEnrollmentsByStudent;
using Enrollments.Application.Queries.GetEnrollmentsBySubject;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Enrollments.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public EnrollmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Lista todas las matrículas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetAllEnrollmentsQuery()));

    /// <summary>Matrículas de un alumno concreto.</summary>
    [HttpGet("student/{studentId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), 200)]
    public async Task<IActionResult> GetByStudent([FromRoute] Guid studentId)
        => Ok(await _mediator.Send(new GetEnrollmentsByStudentQuery(studentId)));

    /// <summary>Matrículas de una asignatura concreta.</summary>
    [HttpGet("subject/{subjectId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), 200)]
    public async Task<IActionResult> GetBySubject([FromRoute] Guid subjectId)
        => Ok(await _mediator.Send(new GetEnrollmentsBySubjectQuery(subjectId)));

    /// <summary>
    /// Matricula un alumno en una asignatura.
    /// Ejecuta: Strategy validation → Factory → Observer (evento).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Enroll([FromBody] EnrollStudentRequest req)
    {
        var result = await _mediator.Send(
            new EnrollStudentCommand(req.StudentId, req.SubjectId, req.Notes));
        return CreatedAtAction(nameof(GetByStudent), new { studentId = result.StudentId }, result);
    }

    /// <summary>Cancela una matrícula activa.</summary>
    [HttpDelete("{enrollmentId:guid}")]
    [ProducesResponseType(typeof(EnrollmentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid enrollmentId,
        [FromBody]  CancelEnrollmentRequest? req = null)
        => Ok(await _mediator.Send(new CancelEnrollmentCommand(enrollmentId, req?.Reason)));
}
