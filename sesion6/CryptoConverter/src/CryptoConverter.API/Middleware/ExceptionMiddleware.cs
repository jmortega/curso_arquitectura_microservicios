using System.Text.Json;
using CryptoConverter.API.Models;

namespace CryptoConverter.API.Middleware;

/// <summary>
/// Middleware que captura excepciones no controladas y devuelve
/// siempre una respuesta JSON consistente con el formato ErrorResponse.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada en {Path}", context.Request.Path);
            await ManejarExcepcionAsync(context, ex);
        }
    }

    private static async Task ManejarExcepcionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = StatusCodes.Status500InternalServerError;

        var error = new ErrorResponse
        {
            Codigo  = "ERROR_INTERNO",
            Mensaje = "Ha ocurrido un error inesperado. Por favor, inténtalo de nuevo.",
        };

        var json = JsonSerializer.Serialize(error,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }
}
