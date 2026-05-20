using AcademyManager.Application.Common.Behaviors;
using AcademyManager.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using RabbitMQ.Client;
using Serilog;
using System.Reflection;
using AcademyManager.Infrastructure.Persistence.Write;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ── Forwarded Headers ─────────────────────────────────────────────────────────
// Debe registrarse ANTES de cualquier otro middleware que use la IP o el scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Application Services ──────────────────────────────────────────────────────
var applicationAssembly = Assembly.Load("AcademyManager.Application");
var infraAssembly       = Assembly.Load("AcademyManager.Infrastructure");

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(applicationAssembly);
    cfg.RegisterServicesFromAssembly(infraAssembly);
});
builder.Services.AddValidatorsFromAssembly(applicationAssembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ── Infrastructure (PostgreSQL, MongoDB, MassTransit+RabbitMQ+Outbox) ─────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── API + Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Academy Manager API",
        Version     = "v1",
        Description = "CQRS + Hexagonal + EDA — Students and Enrollments management"
    });
});

// ── RabbitMQ IConnection singleton ────────────────────────────────────────────
//
//  AspNetCore.HealthChecks.Rabbitmq.v6 v9.0.0 cambió la API:
//  ya no acepta la cadena de conexión directamente en AddRabbitMQ().
//  Ahora requiere registrar IConnection como singleton para que el health check
//  lo resuelva desde el contenedor DI. RabbitMQ recomienda reutilizar la
//  conexión (long-lived connection) en lugar de crearla en cada comprobación.
//
var rabbitUri = builder.Configuration["RabbitMQ:ConnectionString"]
    ?? "amqp://guest:guest@rabbitmq:5672";

builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        Uri                        = new Uri(rabbitUri),
        AutomaticRecoveryEnabled   = true,
        UseBackgroundThreadsForIO  = true
    };
    return factory.CreateConnection();
});

// ── Health checks ─────────────────────────────────────────────────────────────
//
//  /healthz/live  → Liveness  (¿está el proceso vivo?)
//  /healthz/ready → Readiness (¿puede recibir tráfico? — verifica PostgreSQL y RabbitMQ)
//
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WriteDbContext>("postgres-write",
        tags: new[] { "ready" })
    .AddRabbitMQ(                    // resuelve IConnection del DI
        name: "rabbitmq",
        tags: new[] { "ready" })
    .ForwardToPrometheus();

// ── Prometheus ────────────────────────────────────────────────────────────────
builder.Services.UseHttpClientMetrics();

// ── OpenTelemetry — Trazabilidad distribuida ──────────────────────────────────
var serviceName         = builder.Configuration["Tracing:ServiceName"]         ?? "academy-manager";
var tracingBackend      = (builder.Configuration["Tracing:Backend"] ?? "jaeger").ToLowerInvariant();
var jaegerEndpoint      = builder.Configuration["Tracing:JaegerEndpoint"]      ?? "http://jaeger:4317";
var zipkinEndpoint      = builder.Configuration["Tracing:ZipkinEndpoint"]      ?? "http://zipkin:9411/api/v2/spans";
var postgresServiceName = builder.Configuration["Tracing:PostgresServiceName"] ?? "postgres-write";
var mongoServiceName    = builder.Configuration["Tracing:MongoServiceName"]    ?? "mongodb-read";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracer =>
    {
        tracer
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName,
                        ["service.version"]        = "1.0.0"
                    }))
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments("/metrics")  &&
                    !ctx.Request.Path.StartsWithSegments("/healthz")  &&
                    !ctx.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    var host = request.RequestUri?.Host ?? string.Empty;
                    if (host.Contains("mongodb"))
                    {
                        activity?.SetTag("peer.service", mongoServiceName);
                        activity?.SetTag("db.system",    "mongodb");
                    }
                };
            })
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true;
                opts.EnrichWithIDbCommand = (activity, command) =>
                {
                    activity?.SetTag("peer.service", postgresServiceName);
                    activity?.SetTag("db.system",    "postgresql");
                    activity?.SetTag("db.name",      "academy_write");
                };
            })
            .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
            .AddSource("MassTransit");

        var useJaeger = tracingBackend is "jaeger" or "both";
        var useZipkin = tracingBackend is "zipkin" or "both";

        if (useJaeger)
            tracer.AddOtlpExporter(opts => opts.Endpoint = new Uri(jaegerEndpoint));
        if (useZipkin)
            tracer.AddZipkinExporter(opts => opts.Endpoint = new Uri(zipkinEndpoint));

        var active = tracingBackend switch
        {
            "both"   => "Jaeger + Zipkin",
            "zipkin" => "Zipkin",
            _        => "Jaeger"
        };
        Console.WriteLine($"[Tracing] Backend  : {active}");
        Console.WriteLine($"[Tracing] Postgres : {postgresServiceName}");
        Console.WriteLine($"[Tracing] MongoDB  : {mongoServiceName}");
    });

var app = builder.Build();

// ── Forwarded Headers: PRIMER middleware del pipeline ─────────────────────────
app.UseForwardedHeaders();

// ── Auto-migrate ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
    db.Database.EnsureCreated();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Academy Manager v1");
    c.RoutePrefix = string.Empty;
});

app.UseSerilogRequestLogging();

app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", _ => "academy-manager");
});

app.MapControllers();

// Liveness: responde 200 si el proceso está vivo (sin verificar dependencias)
app.MapHealthChecks("/healthz/live", new()
{
    Predicate = _ => false
});

// Readiness: verifica PostgreSQL y RabbitMQ antes de aceptar tráfico
app.MapHealthChecks("/healthz/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health");
app.MapMetrics("/metrics");

app.Run();
