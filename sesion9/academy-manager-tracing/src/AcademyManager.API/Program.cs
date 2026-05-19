using AcademyManager.Application.Common.Behaviors;
using AcademyManager.Infrastructure;
using FluentValidation;
using MediatR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using System.Reflection;
using AcademyManager.Infrastructure.Persistence.Write;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

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

// ── Infrastructure ────────────────────────────────────────────────────────────
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
        Description = "CQRS + Hexagonal Architecture demo — Students and Enrollments management"
    });
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WriteDbContext>("postgres-write")
    .ForwardToPrometheus();

// ── Prometheus ────────────────────────────────────────────────────────────────
builder.Services.UseHttpClientMetrics();

// ── OpenTelemetry — Trazabilidad distribuida ──────────────────────────────────
//
//  Las bases de datos (PostgreSQL, MongoDB) no tienen SDK de OpenTelemetry:
//  no pueden enviar sus propios spans a Jaeger.
//
//  La solución es etiquetar los spans que la API genera al llamar a las BBDDs
//  con dos atributos clave que Jaeger entiende:
//
//    peer.service  → nombre del servicio remoto (aparece en el grafo y en la lista)
//    db.system     → tipo de base de datos ("postgresql" | "mongodb")
//    db.name       → nombre de la base de datos concreta
//
//  Con estos tags, Jaeger dibuja:
//    academy-manager ──► postgres-write
//    academy-manager ──► mongodb-read
//
//  Los nombres (peer.service) son configurables desde docker-compose.yml:
//    Tracing__PostgresServiceName
//    Tracing__MongoServiceName
//
var serviceName        = builder.Configuration["Tracing:ServiceName"]        ?? "academy-manager";
var tracingBackend     = (builder.Configuration["Tracing:Backend"] ?? "jaeger").ToLowerInvariant();
var jaegerEndpoint     = builder.Configuration["Tracing:JaegerEndpoint"]     ?? "http://jaeger:4317";
var zipkinEndpoint     = builder.Configuration["Tracing:ZipkinEndpoint"]     ?? "http://zipkin:9411/api/v2/spans";
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

            // ── Peticiones HTTP entrantes ─────────────────────────────────────
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments("/metrics") &&
                    !ctx.Request.Path.StartsWithSegments("/health");
            })

            // ── Peticiones HTTP salientes (HttpClient) ────────────────────────
            .AddHttpClientInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    var host = request.RequestUri?.Host ?? string.Empty;
                    if (host.Contains("mongodb"))
                    {
                        // Identificar el servicio MongoDB para el grafo de dependencias
                        activity?.SetTag("peer.service", mongoServiceName);
                        activity?.SetTag("db.system",    "mongodb");
                    }
                };
            })

            // ── Entity Framework Core → PostgreSQL ────────────────────────────
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true;
                opts.EnrichWithIDbCommand = (activity, command) =>
                {
                    // peer.service: nombre con el que aparecerá PostgreSQL en Jaeger
                    // (se lee de Tracing__PostgresServiceName en docker-compose.yml)
                    activity?.SetTag("peer.service", postgresServiceName);
                    activity?.SetTag("db.system",    "postgresql");
                    activity?.SetTag("db.name",      "academy_write");
                };
            })

            // ── MongoDB Driver ────────────────────────────────────────────────
            // Captura find/insert/update/delete como spans.
            // DependencyInjection.cs añade el DiagnosticsActivityEventSubscriber
            // al MongoClient para que el driver emita estas actividades.
            .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources");

        // ── Exporters ─────────────────────────────────────────────────────────
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
        Console.WriteLine($"[Tracing] Postgres  : {postgresServiceName}");
        Console.WriteLine($"[Tracing] MongoDB   : {mongoServiceName}");
    });

var app = builder.Build();

// ── Auto-migrate ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
    db.Database.EnsureCreated();
}

// ── Middleware ────────────────────────────────────────────────────────────────
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
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");

app.Run();
