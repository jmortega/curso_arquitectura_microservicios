using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Students.Infrastructure.Messaging;

// ── Configuración ─────────────────────────────────────────────────────────────

public sealed class RabbitMqSettings
{
    public string Host        { get; set; } = "localhost";
    public int    Port        { get; set; } = 5672;
    public string User        { get; set; } = "guest";
    public string Password    { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange    { get; set; } = "students-events";
}

// ── Interfaz del publicador ───────────────────────────────────────────────────

public interface IStudentEventPublisher
{
    Task PublishStudentDeactivatedAsync(Guid studentId, string fullName, string email);
    Task PublishStudentCreatedAsync(Guid studentId, string firstName, string lastName,
                                    string email, string enrollmentNumber);
}

// ── Implementación RabbitMQ ───────────────────────────────────────────────────

public sealed class RabbitMqStudentEventPublisher : IStudentEventPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqStudentEventPublisher> _logger;

    private IConnection? _connection;
    private IModel?      _channel;

    // IModel no es thread-safe — usamos semáforo para publicaciones concurrentes
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqStudentEventPublisher(
        RabbitMqSettings settings,
        ILogger<RabbitMqStudentEventPublisher> logger)
    {
        _settings = settings;
        _logger   = logger;

        // Conexión eagler al construir el singleton
        TryConnect();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public Task PublishStudentDeactivatedAsync(Guid studentId, string fullName, string email)
    {
        var payload = new
        {
            StudentId   = studentId,
            StudentName = fullName,
            Email       = email,
            OccurredAt  = DateTime.UtcNow,
        };
        return PublishAsync("student.deactivated", payload);
    }

    public Task PublishStudentCreatedAsync(Guid studentId, string firstName, string lastName,
                                           string email, string enrollmentNumber)
    {
        var payload = new
        {
            StudentId        = studentId,
            FirstName        = firstName,
            LastName         = lastName,
            Email            = email,
            EnrollmentNumber = enrollmentNumber,
            OccurredAt       = DateTime.UtcNow,
        };
        return PublishAsync("student.created", payload);
    }

    // ── Publicación con reconexión automática ─────────────────────────────────

    private async Task PublishAsync(string routingKey, object payload)
    {
        await _lock.WaitAsync();
        try
        {
            // Reconectar si el canal está cerrado o es nulo
            EnsureChannelOpen();

            var body  = JsonSerializer.SerializeToUtf8Bytes(payload);
            var props = _channel!.CreateBasicProperties();
            props.Persistent  = true;
            props.ContentType = "application/json";

            _channel.BasicPublish(
                exchange:        _settings.Exchange,
                routingKey:      routingKey,
                basicProperties: props,
                body:            body);

            _logger.LogInformation(
                "[RABBITMQ] ✓ Publicado '{RoutingKey}' → exchange '{Exchange}'",
                routingKey, _settings.Exchange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RABBITMQ] ✗ Error publicando '{RoutingKey}' — invalidando canal",
                routingKey);

            // Invalidar para forzar reconexión en el siguiente intento
            CloseChannel();
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Gestión de conexión ───────────────────────────────────────────────────

    private void EnsureChannelOpen()
    {
        if (_channel is { IsOpen: true }) return;

        _logger.LogWarning("[RABBITMQ] Canal cerrado o nulo — reconectando...");
        TryConnect();
    }

    private void TryConnect()
    {
        try
        {
            CloseChannel();

            var factory = new ConnectionFactory
            {
                HostName                 = _settings.Host,
                Port                     = _settings.Port,
                UserName                 = _settings.User,
                Password                 = _settings.Password,
                VirtualHost              = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval  = TimeSpan.FromSeconds(5),
            };

            _connection = factory.CreateConnection("students-service-publisher");
            _channel    = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: _settings.Exchange,
                type:     ExchangeType.Topic,
                durable:  true);

            _logger.LogInformation(
                "[RABBITMQ] ✓ Publisher conectado al exchange '{Exchange}' en {Host}:{Port}",
                _settings.Exchange, _settings.Host, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RABBITMQ] ✗ No se pudo conectar a RabbitMQ ({Host}:{Port})",
                _settings.Host, _settings.Port);
            // No lanzar — el servicio arranca aunque RabbitMQ no esté disponible todavía
        }
    }

    private void CloseChannel()
    {
        try { _channel?.Close();    } catch { /* ignorar en cierre */ }
        try { _connection?.Close(); } catch { /* ignorar en cierre */ }
        _channel    = null;
        _connection = null;
    }

    public void Dispose()
    {
        _lock.Dispose();
        CloseChannel();
    }
}
