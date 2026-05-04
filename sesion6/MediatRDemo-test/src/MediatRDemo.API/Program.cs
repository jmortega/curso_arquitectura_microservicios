using FluentValidation;
using MediatR;
using MediatRDemo.API.Application.Behaviors;
using MediatRDemo.API.Infrastructure;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "MediatR Demo API",
        Version     = "v1",
        Description = """
            Demo del patrón **Mediator** con MediatR + MySQL + Docker.

            ## Pipeline de MediatR
            ```
            Request → LoggingBehavior → ValidationBehavior → Handler → Response
            ```

            ## Tipos de mensajes
            - **IRequest** — Commands y Queries (un handler)
            - **INotification** — Eventos de dominio (N handlers)
            - **IPipelineBehavior** — Cross-cutting concerns
            """,
    });
});

// ── MySQL + EF Core ───────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionString 'Default' not found.");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr),
        x => x.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

// ── MediatR + Pipeline behaviors ─────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

var app = builder.Build();

// ── Aplicar migraciones y seed al arrancar ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediatR Demo v1");
    c.RoutePrefix   = string.Empty;
    c.DocumentTitle = "MediatR Demo API";
    c.DisplayRequestDuration();
});

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Necesario para WebApplicationFactory en tests
public partial class Program { }
