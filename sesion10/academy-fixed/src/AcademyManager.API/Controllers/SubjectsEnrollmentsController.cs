using AcademyManager.Application.Subjects.Commands.CreateSubject;
using AcademyManager.Application.Enrollments.Commands.EnrollStudent;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using AcademyManager.Application.Enrollments.Commands.EnrollStudent.AcademyManager.Application.Enrollments.Commands.CancelEnrollment.AcademyManager.Application.Enrollments.Queries.GetStudentEnrollments;
using AcademyManager.Application.Enrollments.Commands.EnrollStudent.AcademyManager.Application.Enrollments.Commands.CancelEnrollment;
using AcademyManager.Application.Subjects.Commands.CreateSubject.AcademyManager.Application.Subjects.Queries.GetAllSubjects;

namespace AcademyManager.API.Controllers;

// ─── Subjects Controller ──────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class SubjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubjectsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get all subjects — reads from MongoDB read model</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAllSubjectsQuery(), ct));

    /// <summary>Create a new subject — writes to PostgreSQL, projects to MongoDB</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSubjectCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }
}

// ─── Enrollments Controller ───────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnrollmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get all enrollments for a student — reads from MongoDB</summary>
    [HttpGet("student/{studentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStudent(Guid studentId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetStudentEnrollmentsQuery(studentId), ct));

    /// <summary>Enroll a student in a subject — writes to PostgreSQL, projects to MongoDB</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll([FromBody] EnrollStudentCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetByStudent), new { studentId = command.StudentId }, new { id });
    }

    /// <summary>Cancel an enrollment — updates PostgreSQL, projects to MongoDB</summary>
    [HttpPut("{enrollmentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid enrollmentId, CancellationToken ct)
    {
        await _mediator.Send(new CancelEnrollmentCommand(enrollmentId), ct);
        return NoContent();
    }
}
