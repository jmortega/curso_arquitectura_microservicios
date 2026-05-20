using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using AcademyManager.Infrastructure.Messaging;
using AcademyManager.Infrastructure.Persistence.Read;
using AcademyManager.Infrastructure.Persistence.Read.AcademyManager.Infrastructure.Persistence.Read.Repositories;
using AcademyManager.Infrastructure.Persistence.Write;
using AcademyManager.Infrastructure.Persistence.Write.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace AcademyManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ── Write DB: PostgreSQL via EF Core ──────────────────────────────────
        services.AddDbContext<WriteDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("WriteDatabase"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(WriteDbContext).Assembly.FullName)));

        services.AddScoped<IStudentRepository,    StudentRepository>();
        services.AddScoped<ISubjectRepository,    SubjectRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        // ── Read DB: MongoDB con instrumentación OpenTelemetry ────────────────
        services.AddSingleton<MongoDbContext>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "MongoDB:ConnectionString is not configured.");

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ClusterConfigurator = cb =>
                cb.Subscribe(new DiagnosticsActivityEventSubscriber(
                    new InstrumentationOptions { CaptureCommandText = true }));

            return new MongoDbContext(config, settings);
        });

        services.AddScoped<IStudentReadRepository,    StudentReadRepository>();
        services.AddScoped<ISubjectReadRepository,    SubjectReadRepository>();
        services.AddScoped<IEnrollmentReadRepository, EnrollmentReadRepository>();

        // ── MassTransit + RabbitMQ + Transactional Outbox ─────────────────────
        //
        //  Flujo:
        //    DomainEvent → MediatR → DomainEventHandler → IPublishEndpoint
        //    → OutboxMessage (PostgreSQL) → Worker → RabbitMQ → Consumer → MongoDB
        //
        services.AddMassTransit(x =>
        {
            // ── Consumers (Read side: actualizan MongoDB) ─────────────────────
            x.AddConsumer<AlumnoMatriculadoConsumer>(cfg =>
            {
                cfg.UseMessageRetry(r => r.Incremental(3,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)));
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod  = TimeSpan.FromSeconds(60);
                    cb.TripThreshold   = 5;
                    cb.ActiveThreshold = 5;
                    cb.ResetInterval   = TimeSpan.FromSeconds(30);
                });
            });

            x.AddConsumer<EnrollmentCompletedConsumer>(cfg =>
                cfg.UseMessageRetry(r => r.Incremental(3,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1))));

            x.AddConsumer<EnrollmentCancelledConsumer>(cfg =>
                cfg.UseMessageRetry(r => r.Incremental(3,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1))));

            x.AddConsumer<StudentCreatedConsumer>(cfg =>
                cfg.UseMessageRetry(r => r.Incremental(3,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1))));

            // ── Transactional Outbox ──────────────────────────────────────────
            //
            //  IPublishEndpoint en los DomainEventHandlers guarda el mensaje
            //  en la tabla OutboxMessage DENTRO de la transacción EF Core activa.
            //  El DeliveryService lo lee y lo envía a RabbitMQ (At-least-once).
            //
            x.AddEntityFrameworkOutbox<WriteDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            // ── RabbitMQ Transport ────────────────────────────────────────────
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitUri = configuration["RabbitMQ:ConnectionString"]
                    ?? "amqp://guest:guest@rabbitmq:5672";

                cfg.Host(rabbitUri);

                cfg.UseMessageRetry(r => r.Incremental(3,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
