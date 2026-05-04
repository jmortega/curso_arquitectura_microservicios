using Enrollments.Application.EventHandlers;
using Enrollments.Domain.Events;
using Enrollments.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Enrollments.Infrastructure.Messaging;

// ════════════════════════════════════════════════════════════════════════════
// CONFIGURACIÓN RABBITMQ
// ════════════════════════════════════════════════════════════════════════════

public sealed class RabbitMqSettings
{
    public string Host         { get; set; } = "localhost";
    public int    Port         { get; set; } = 5672;
    public string User         { get; set; } = "guest";
    public string Password     { get; set; } = "guest";
    public string VirtualHost  { get; set; } = "/";

    // Exchanges
    public string EnrollmentsExchange { get; set; } = "enrollments-events";
    public string StudentsExchange    { get; set; } = "students-events";

    // Colas que consume este servicio
    public string StudentDeactivatedQueue { get; set; } = "enrollments.student-deactivated";
    public string StudentCreatedQueue     { get; set; } = "enrollments.student-created";
}

// ════════════════════════════════════════════════════════════════════════════
// PUBLICADOR DE EVENTOS — PATRÓN OBSERVER (Concrete Subject)
// Implementa IEventPublisher publicando mensajes a RabbitMQ.
// ════════════════════════════════════════════════════════════════════════════

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel      _channel;
    private readonly string      _exchange;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(RabbitMqSettings settings, ILogger<RabbitMqEventPublisher> logger)
    {
        _logger   = logger;
        _exchange = settings.EnrollmentsExchange;

        var factory = new ConnectionFactory
        {
            HostName    = settings.Host,
            Port        = settings.Port,
            UserName    = settings.User,
            Password    = settings.Password,
            VirtualHost = settings.VirtualHost,
        };

        _connection = factory.CreateConnection("enrollments-service-publisher");
        _channel    = _connection.CreateModel();

        // Declarar exchange de tipo topic para routing flexible
        _channel.ExchangeDeclare(
            exchange: _exchange,
            type:     ExchangeType.Topic,
            durable:  true);
    }

    public Task PublishAsync<T>(T domainEvent) where T : IDomainEvent
    {
        try
        {
            var body       = JsonSerializer.SerializeToUtf8Bytes(domainEvent);
            var routingKey = domainEvent.EventType;   // ej. "enrollment.created"

            var props = _channel.CreateBasicProperties();
            props.Persistent   = true;
            props.ContentType  = "application/json";
            props.Headers      = new Dictionary<string, object>
            {
                ["event-type"] = domainEvent.EventType,
                ["event-id"]   = domainEvent.EventId.ToString(),
            };

            _channel.BasicPublish(
                exchange:   _exchange,
                routingKey: routingKey,
                basicProperties: props,
                body:       body);

            _logger.LogInformation(
                "[RABBITMQ] Publicado '{EventType}' (id={EventId})",
                domainEvent.EventType, domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RABBITMQ] Error publicando evento {EventType}", typeof(T).Name);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}

// ════════════════════════════════════════════════════════════════════════════
// CONSUMIDOR DE EVENTOS — PATRÓN OBSERVER (Concrete Observer)
// Escucha eventos publicados por el servicio de estudiantes.
// ════════════════════════════════════════════════════════════════════════════

public sealed class RabbitMqConsumerService : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger<RabbitMqConsumerService> _logger;

    private IConnection? _connection;
    private IModel?      _channel;

    public RabbitMqConsumerService(
        RabbitMqSettings settings,
        IServiceProvider services,
        ILogger<RabbitMqConsumerService> logger)
    {
        _settings = settings;
        _services = services;
        _logger   = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConnectAndConsume();
        return Task.CompletedTask;
    }

    private void ConnectAndConsume()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName                 = _settings.Host,
                Port                     = _settings.Port,
                UserName                 = _settings.User,
                Password                 = _settings.Password,
                VirtualHost              = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                // ⚠️ REQUERIDO para que AsyncEventingBasicConsumer dispare los callbacks
                DispatchConsumersAsync   = true,
            };

            _connection = factory.CreateConnection("enrollments-service-consumer");
            _channel    = _connection.CreateModel();

            // Exchange del servicio de estudiantes
            _channel.ExchangeDeclare(
                exchange: _settings.StudentsExchange,
                type:     ExchangeType.Topic,
                durable:  true);

            // Cola para student.deactivated
            _channel.QueueDeclare(
                queue:      _settings.StudentDeactivatedQueue,
                durable:    true,
                exclusive:  false,
                autoDelete: false);
            _channel.QueueBind(
                queue:      _settings.StudentDeactivatedQueue,
                exchange:   _settings.StudentsExchange,
                routingKey: "student.deactivated");

            // Cola para student.created
            _channel.QueueDeclare(
                queue:      _settings.StudentCreatedQueue,
                durable:    true,
                exclusive:  false,
                autoDelete: false);
            _channel.QueueBind(
                queue:      _settings.StudentCreatedQueue,
                exchange:   _settings.StudentsExchange,
                routingKey: "student.created");

            _channel.BasicQos(0, 1, false);

            // Consumer para student.deactivated
            var deactivatedConsumer = new AsyncEventingBasicConsumer(_channel);
            deactivatedConsumer.Received += OnStudentDeactivatedAsync;
            _channel.BasicConsume(
                queue:       _settings.StudentDeactivatedQueue,
                autoAck:     false,
                consumer:    deactivatedConsumer);

            _logger.LogInformation("[RABBITMQ] Consumidor iniciado — escuchando eventos de students-service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RABBITMQ] Error al conectar el consumidor");
        }
    }

    private async Task OnStudentDeactivatedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            _logger.LogInformation(
                "[RABBITMQ] ← Mensaje recibido en '{Queue}': {Json}",
                _settings.StudentDeactivatedQueue, json);

            var evt = JsonSerializer.Deserialize<StudentDeactivatedEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt is null)
            {
                _logger.LogWarning("[RABBITMQ] Mensaje deserializado como null — descartando");
                _channel!.BasicAck(ea.DeliveryTag, false);
                return;
            }

            _logger.LogInformation(
                "[RABBITMQ] Procesando student.deactivated para StudentId={StudentId} ({Name})",
                evt.StudentId, evt.StudentName);

            using var scope   = _services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<StudentDeactivatedEventHandler>();
            await handler.HandleAsync(evt);

            _channel!.BasicAck(ea.DeliveryTag, false);
            _logger.LogInformation("[RABBITMQ] ✓ Mensaje procesado y confirmado (ACK)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RABBITMQ] ✗ Error procesando student.deactivated — NACK (requeue)");
            _channel!.BasicNack(ea.DeliveryTag, false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
