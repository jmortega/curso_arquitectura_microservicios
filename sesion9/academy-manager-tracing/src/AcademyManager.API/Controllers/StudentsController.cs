using AcademyManager.Application.Students.Commands.CreateStudent;
using AcademyManager.Application.Students.Commands.UpdateStudent;
using AcademyManager.Application.Students.Queries.GetStudentById;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using AcademyManager.Application.Students.Queries.GetStudentById.AcademyManager.Application.Students.Queries.GetAllStudents;
using AcademyManager.Application.Students.Commands.UpdateStudent.AcademyManager.Application.Students.Commands.DeleteStudent;

namespace AcademyManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get all students (paginated) — reads from MongoDB read model</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAllStudentsQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Get a single student by ID — reads from MongoDB read model</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var student = await _mediator.Send(new GetStudentByIdQuery(id), ct);
        return student is null ? NotFound() : Ok(student);
    }

    /// <summary>Create a new student — writes to PostgreSQL, projects to MongoDB</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStudentCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Update an existing student — writes to PostgreSQL, updates MongoDB projection</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateStudentCommand(id, request.FirstName, request.LastName, request.Email), ct);
        return NoContent();
    }

    /// <summary>Delete a student — removes from PostgreSQL and MongoDB</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteStudentCommand(id), ct);
        return NoContent();
    }
}

public sealed record UpdateStudentRequest(string FirstName, string LastName, string Email);
