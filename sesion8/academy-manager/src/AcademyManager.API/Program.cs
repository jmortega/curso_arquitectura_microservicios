using AcademyManager.Application.Common.Behaviors;
using AcademyManager.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using AcademyManager.Infrastructure.Persistence.Write;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ─────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ── Application Services ─────────────────────────────────────────────────────
var applicationAssembly = Assembly.Load("AcademyManager.Application");
var infraAssembly = Assembly.Load("AcademyManager.Infrastructure");

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(applicationAssembly);
    cfg.RegisterServicesFromAssembly(infraAssembly); // event handlers
});

builder.Services.AddValidatorsFromAssembly(applicationAssembly);

// MediatR pipeline behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ── Infrastructure (adapters) ─────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Academy Manager API",
        Version = "v1",
        Description = "CQRS + Hexagonal Architecture demo — Students and Enrollments management"
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<WriteDbContext>("postgres-write");

var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
    // EnsureCreated crea las tablas directamente desde el modelo EF
    // sin necesitar ficheros de migración generados
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
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
