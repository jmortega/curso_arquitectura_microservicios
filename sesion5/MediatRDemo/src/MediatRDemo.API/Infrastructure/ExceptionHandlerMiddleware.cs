using FluentValidation;
using MediatRDemo.API.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace MediatRDemo.API.Infrastructure;

/// <summary>
/// Middleware de manejo global de excepciones.
/// Convierte las excepciones de dominio en respuestas HTTP apropiadas.
/// </summary>
public sealed class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            UserNotFoundException      e => (HttpStatusCode.NotFound,       e.Message, (object?)null),
            EmailAlreadyExistsException e => (HttpStatusCode.Conflict,       e.Message, (object?)null),
            ValidationException         e => (HttpStatusCode.BadRequest,
                                              "Errores de validación.",
                                              e.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })),
            ArgumentException           e => (HttpStatusCode.BadRequest,     e.Message, (object?)null),
            _                             => (HttpStatusCode.InternalServerError, "Error interno del servidor.", (object?)null),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Error no controlado: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            status  = (int)statusCode,
            message,
            errors,
            traceId = context.TraceIdentifier,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}
