using MediatR;
using System.Diagnostics;

namespace MediatRDemo.API.Application.Behaviors;

/// <summary>
/// Pipeline Behavior de MediatR — se ejecuta automáticamente para TODAS
/// las requests antes y después del handler correspondiente.
///
/// Es el equivalente al middleware de ASP.NET Core pero para el pipeline
/// de MediatR. Aquí implementamos logging y medición de tiempo.
///
/// Orden de ejecución:
///   Request → LoggingBehavior → ValidationBehavior → Handler → Response
/// </summary>
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
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw          = Stopwatch.StartNew();

        _logger.LogInformation(
            "[PIPELINE] → Procesando {Request}", requestName);

        try
        {
            var response = await next();

            sw.Stop();
            _logger.LogInformation(
                "[PIPELINE] ← {Request} completada en {Ms}ms", requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[PIPELINE] ✗ {Request} falló después de {Ms}ms — {Error}",
                requestName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
