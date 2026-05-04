using Enrollments.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Enrollments.API.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, msg, errors) = ex switch
        {
            SubjectNotFoundException         e => (HttpStatusCode.NotFound,    e.Message, (object?)null),
            EnrollmentNotFoundException      e => (HttpStatusCode.NotFound,    e.Message, null),
            StudentNotFoundException         e => (HttpStatusCode.NotFound,    e.Message, null),
            SubjectCodeAlreadyExistsException e => (HttpStatusCode.Conflict,   e.Message, null),
            DuplicateEnrollmentException     e => (HttpStatusCode.Conflict,    e.Message, null),
            EnrollmentValidationException    e => (HttpStatusCode.BadRequest,  e.Message, null),
            ValidationException              e => (HttpStatusCode.BadRequest,
                                                  "Errores de validación.",
                                                  e.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })),
            ArgumentException                e => (HttpStatusCode.BadRequest,  e.Message, null),
            _                                  => (HttpStatusCode.InternalServerError, "Error interno.", null),
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Error no controlado: {Msg}", ex.Message);

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)status;

        var body = JsonSerializer.Serialize(new
        {
            status  = (int)status,
            message = msg,
            errors,
            traceId = ctx.TraceIdentifier,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await ctx.Response.WriteAsync(body);
    }
}
