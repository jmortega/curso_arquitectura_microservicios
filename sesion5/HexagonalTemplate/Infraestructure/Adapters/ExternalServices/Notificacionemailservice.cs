namespace GestionAcademica.Infrastructure.Adapters.ExternalServices;

using GestionAcademica.Domain.Ports;

/// <summary>
/// Adaptador de servicio externo — cliente para APIs externas o modelos de IA.
/// Implementa el puerto INotificacionService.
/// En producción se reemplazaría por SendGrid, Azure Communication, etc.
/// </summary>
public class NotificacionEmailService : INotificacionService
{
    private readonly ILogger<NotificacionEmailService> _logger;

    public NotificacionEmailService(ILogger<NotificacionEmailService> logger)
        => _logger = logger;

    public Task EnviarEmailAsync(string destinatario, string asunto,
                                  string cuerpo, CancellationToken ct)
    {
        // Simulación — en producción llamaría a la API de email real
        _logger.LogInformation(
            """
            ┌─ EMAIL SIMULADO ──────────────────────────────┐
              Para:   {Destinatario}
              Asunto: {Asunto}
              Cuerpo: {Cuerpo}
            └───────────────────────────────────────────────┘
            """,
            destinatario, asunto, cuerpo);

        return Task.CompletedTask;
    }
}