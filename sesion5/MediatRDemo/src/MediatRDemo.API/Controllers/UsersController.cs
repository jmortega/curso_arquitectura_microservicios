using MediatR;
using MediatRDemo.API.Features.Common;
using MediatRDemo.API.Features.Users.Commands;
using MediatRDemo.API.Features.Users.Events;
using MediatRDemo.API.Features.Users.Queries;
using Microsoft.AspNetCore.Mvc;

namespace MediatRDemo.API.Controllers;

/// <summary>
/// Controlador de usuarios — demuestra el patrón Mediator con MediatR.
///
/// El controlador es DELGADO: no contiene lógica de negocio.
/// Solo:
///   1. Recibe la petición HTTP
///   2. Construye el Command/Query correspondiente
///   3. Lo envía al mediador: await _mediator.Send(request)
///   4. Devuelve el resultado
///
/// Toda la lógica vive en los Handlers.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    // ── QUERIES ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todos los usuarios.
    /// Handler: GetAllUsersQueryHandler
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool?   onlyActive = null,
        [FromQuery] string? role       = null)
        => Ok(await _mediator.Send(new GetAllUsersQuery(onlyActive, role)));

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// Handler: GetUserByIdQueryHandler
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
        => Ok(await _mediator.Send(new GetUserByIdQuery(id)));

    // ── COMMANDS ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo usuario.
    /// Handler: CreateUserCommandHandler
    /// Tras la creación publica UserCreatedNotification → 3 handlers en paralelo.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        var user = await _mediator.Send(command);

        // Publicar evento de dominio — ejecuta TODOS los INotificationHandler registrados
        await _mediator.Publish(new UserCreatedNotification(
            user.Id, user.Name, user.Email, user.Role));

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// Actualiza los datos de un usuario.
    /// Handler: UpdateUserCommandHandler
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody]  UpdateUserBody body)
        => Ok(await _mediator.Send(new UpdateUserCommand(id, body.Name, body.Email, body.Role)));

    /// <summary>
    /// Activa un usuario.
    /// Handler: ToggleUserActiveHandler
    /// </summary>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Activate([FromRoute] Guid id)
    {
        var isActive = await _mediator.Send(new ToggleUserActiveCommand(id, Activate: true));
        return Ok(new { id, isActive, message = "Usuario activado." });
    }

    /// <summary>
    /// Desactiva un usuario.
    /// Handler: ToggleUserActiveHandler
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id)
    {
        var isActive = await _mediator.Send(new ToggleUserActiveCommand(id, Activate: false));
        return Ok(new { id, isActive, message = "Usuario desactivado." });
    }

    /// <summary>
    /// Elimina un usuario.
    /// Handler: DeleteUserCommandHandler
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteUserCommand(id));
        return NoContent();
    }
}

// ── Request body para Update (evita conflicto con el Guid de la ruta) ─────────
public record UpdateUserBody(string Name, string Email, string Role = "User");
