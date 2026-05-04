using FluentValidation;
using MediatR;

namespace MediatRDemo.API.Application.Behaviors;

/// <summary>
/// Pipeline Behavior que ejecuta todos los validadores de FluentValidation
/// registrados para una request antes de llamar al handler.
///
/// Si la validación falla, lanza ValidationException y el handler NUNCA se ejecuta.
/// Esto elimina la necesidad de validar manualmente en cada handler.
/// </summary>
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
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        // Ejecutar todos los validadores y recopilar errores
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
