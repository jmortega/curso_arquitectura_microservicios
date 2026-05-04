using FluentValidation;
using MediatR;
using System.Diagnostics;

namespace Enrollments.Application.Behaviors;

// ════════════════════════════════════════════════════════════════════════════
// PATRÓN DECORATOR implementado como MediatR IPipelineBehavior
//
// LoggingBehavior envuelve TODOS los handlers sin modificarlos.
// Es el patrón Decorator: añade comportamiento (logging/timing) transparentemente.
//
// Pipeline de ejecución:
//   Request → LoggingBehavior → ValidationBehavior → Handler → Response
// ════════════════════════════════════════════════════════════════════════════

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw   = Stopwatch.StartNew();

        _logger.LogInformation("[MEDIATOR] → {Request} iniciado", name);

        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation("[MEDIATOR] ✓ {Request} completado en {Ms}ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[MEDIATOR] ✗ {Request} falló en {Ms}ms — {Error}",
                name, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}

// ── ValidationBehavior ────────────────────────────────────────────────────────

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var ctx      = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(ctx))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0) throw new ValidationException(failures);

        return await next();
    }
}
