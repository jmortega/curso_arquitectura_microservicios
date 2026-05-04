using Microsoft.AspNetCore.Mvc;
using Students.Application.DTOs;
using Students.Application.UseCases;

namespace Students.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger      = logger;
    }

    /// <summary>Registra un nuevo usuario en el sistema.</summary>
    /// <remarks>
    /// Roles disponibles: **Admin**, **Teacher**, **ReadOnly**
    ///
    /// - **Admin**: acceso completo (CRUD)
    /// - **Teacher**: puede crear y actualizar alumnos
    /// - **ReadOnly**: solo puede consultar
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            _logger.LogInformation("Usuario registrado: {Username} con rol {Role}",
                request.Username, request.Role);
            return StatusCode(201, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message, 409));
        }
    }

    /// <summary>Autentica un usuario y devuelve un token JWT.</summary>
    /// <remarks>
    /// Incluye el token recibido en la cabecera de las siguientes peticiones:
    ///
    ///     Authorization: Bearer {token}
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            _logger.LogInformation("Login correcto: {Username}", request.Username);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Intento de login fallido: {Username}", request.Username);
            return Unauthorized(new ErrorResponse(ex.Message, 401));
        }
    }
}
