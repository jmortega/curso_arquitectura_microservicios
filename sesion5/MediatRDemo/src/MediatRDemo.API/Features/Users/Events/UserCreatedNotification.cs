using MediatR;

namespace MediatRDemo.API.Features.Users.Events;

// ── Notification (Evento de dominio) ─────────────────────────────────────────

/// <summary>
/// Notificación publicada cuando se crea un usuario.
/// A diferencia de IRequest, INotification permite MÚLTIPLES handlers.
/// El mediador los ejecuta todos cuando se hace mediator.Publish(notification).
/// </summary>
public record UserCreatedNotification(
    Guid   UserId,
    string Name,
    string Email,
    string Role
) : INotification;

// ── Handler 1: Log ────────────────────────────────────────────────────────────

/// <summary>
/// Primer handler: registra el evento en el log.
/// Ambos handlers se ejecutan en paralelo para el mismo evento.
/// </summary>
public sealed class LogUserCreatedHandler
    : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<LogUserCreatedHandler> _logger;

    public LogUserCreatedHandler(ILogger<LogUserCreatedHandler> logger)
        => _logger = logger;

    public Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[EVENT] UserCreated → Id={Id}, Name={Name}, Email={Email}, Role={Role}",
            notification.UserId, notification.Name,
            notification.Email,  notification.Role);

        return Task.CompletedTask;
    }
}

// ── Handler 2: Welcome email (simulado) ──────────────────────────────────────

/// <summary>
/// Segundo handler: simula el envío de un email de bienvenida.
/// Demuestra que un mismo evento puede tener N handlers independientes.
/// </summary>
public sealed class SendWelcomeEmailHandler
    : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(ILogger<SendWelcomeEmailHandler> logger)
        => _logger = logger;

    public async Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        // Simulación de envío de email (en producción aquí iría el servicio real)
        await Task.Delay(10, ct);
        _logger.LogInformation(
            "[EMAIL] Bienvenida enviada a {Email} para el usuario '{Name}'.",
            notification.Email, notification.Name);
    }
}

// ── Handler 3: Auditoría ──────────────────────────────────────────────────────

public sealed class AuditUserCreatedHandler
    : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<AuditUserCreatedHandler> _logger;

    public AuditUserCreatedHandler(ILogger<AuditUserCreatedHandler> logger)
        => _logger = logger;

    public Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[AUDIT] Nuevo usuario registrado — Id={Id}, Rol={Role}, Timestamp={Ts:O}",
            notification.UserId, notification.Role, DateTime.UtcNow);

        return Task.CompletedTask;
    }
}
