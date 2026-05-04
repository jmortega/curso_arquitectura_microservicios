using Enrollments.Application.Behaviors;
using Enrollments.Application.EventHandlers;
using Enrollments.Domain.Interfaces;
using Enrollments.Domain.Strategies;
using Enrollments.Infrastructure.HttpClients;
using Enrollments.Infrastructure.Messaging;
using Enrollments.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MediatR;
using System.Reflection;

namespace Enrollments.Infrastructure.Configuration;

public static class ServiceExtensions
{
    public static IServiceCollection AddEnrollmentsServices(
        this IServiceCollection services,
        IConfiguration           configuration)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not configured.");

        // ── PATRÓN REPOSITORY — registrar adaptadores de datos ────────────────
        services.AddScoped<ISubjectRepository>(    _ => new SubjectRepository(connStr));
        services.AddScoped<IEnrollmentRepository>( _ => new EnrollmentRepository(connStr));

        // ── PATRÓN STRATEGY — registrar estrategias de validación ─────────────
        // Se registran las implementaciones concretas de IEnrollmentValidationStrategy.
        // El EnrollStudentCommandHandler recibirá IEnumerable<IEnrollmentValidationStrategy>
        // con TODAS las estrategias registradas aquí.
        services.AddScoped<IEnrollmentValidationStrategy, ActiveStudentValidationStrategy>();
        services.AddScoped<IEnrollmentValidationStrategy, ActiveSubjectValidationStrategy>();
        services.AddScoped<IEnrollmentValidationStrategy, CapacityValidationStrategy>();

        // ── MEDIATOR (MediatR) ────────────────────────────────────────────────
        // Registra todos los Commands, Queries y Handlers del ensamblado.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // PATRÓN DECORATOR (logging): envuelve todos los handlers
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            // Validación automática con FluentValidation
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // ── FLUENT VALIDATION ─────────────────────────────────────────────────
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── HTTP CLIENT — consultar servicio de estudiantes ───────────────────
        var studentsUrl = configuration["Services:StudentsServiceUrl"]
            ?? "http://students-api:8080";

        services.AddHttpClient<IStudentServiceClient, StudentServiceHttpClient>(c =>
        {
            c.BaseAddress = new Uri(studentsUrl);
            c.Timeout     = TimeSpan.FromSeconds(10);
        });

        // ── RABBITMQ ──────────────────────────────────────────────────────────
        var rabbitSettings = new RabbitMqSettings();
        configuration.GetSection("RabbitMq").Bind(rabbitSettings);
        services.AddSingleton(rabbitSettings);

        // PATRÓN OBSERVER (publicador) — singleton para reutilizar la conexión
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // PATRÓN OBSERVER (consumidor) — background service que escucha eventos
        services.AddHostedService<RabbitMqConsumerService>();

        // Handler de eventos externos del servicio de estudiantes
        services.AddScoped<StudentDeactivatedEventHandler>();

        return services;
    }

    public static IServiceCollection AddSwaggerWithDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title       = "Enrollments Service API",
                Version     = "v1",
                Description = """
                    Microservicio de **Gestión de Matrículas** — Arquitectura Hexagonal.

                    ## Patrones de diseño aplicados

                    | Patrón | Dónde |
                    |--------|-------|
                    | **Repository** | ISubjectRepository / IEnrollmentRepository — abstrae Dapper+MySQL |
                    | **Strategy** | IEnrollmentValidationStrategy — reglas de validación intercambiables |
                    | **Factory** | SubjectFactory / EnrollmentFactory — construcción de entidades |
                    | **Observer** | IEventPublisher → RabbitMQ → StudentDeactivatedEventHandler |
                    | **Decorator** | LoggingBehavior (IPipelineBehavior) — logging transversal |
                    | **Mediator** | MediatR Commands/Queries/Handlers — desacoplamiento de casos de uso |

                    ## Comunicación asíncrona con Students-Service
                    - **Publica** → `enrollments-events` exchange (enrollment.created, enrollment.cancelled)
                    - **Consume** ← `students-events` exchange (student.deactivated, student.created)
                    """,
            });
        });
        return services;
    }
}
