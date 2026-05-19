using AcademyManager.Application.Common.Behaviors;
using AcademyManager.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    .ForwardToPrometheus();           // expone el resultado del health check como métrica

// ── Prometheus: métricas de runtime .NET ─────────────────────────────────────
builder.Services.UseHttpClientMetrics(); // métricas de HttpClient saliente

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

// Middleware de prometheus: intercepta todas las peticiones HTTP y registra:
//   - http_requests_total          (contador por ruta, método, status)
//   - http_request_duration_seconds (histograma de latencia)
//   - http_requests_in_progress    (gauge de peticiones en curso)
app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", _ => "academy-manager");
});

app.MapControllers();
app.MapHealthChecks("/health");

// Endpoint /metrics que Prometheus scraped cada 15s
app.MapMetrics("/metrics");

app.Run();
