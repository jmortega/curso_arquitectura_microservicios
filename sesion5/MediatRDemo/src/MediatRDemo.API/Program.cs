using FluentValidation;
using MediatR;
using MediatRDemo.API.Application.Behaviors;
using MediatRDemo.API.Infrastructure;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── Controladores ─────────────────────────────────────────────────────────────
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
            Demo del patrón **Mediator** con la librería **MediatR** en .NET 8.

            ## Arquitectura

            ```
            HTTP Request
                │
                ▼
            Controller (delgado — solo dispatching)
                │  mediator.Send(command/query)
                ▼
            MediatR Pipeline:
              LoggingBehavior → ValidationBehavior → Handler
                │
                ▼
            Response

            mediator.Publish(notification)
                │
                ├──► LogUserCreatedHandler
                ├──► SendWelcomeEmailHandler
                └──► AuditUserCreatedHandler
            ```

            ## Tipos de mensajes MediatR
            - **IRequest** — Commands y Queries (un handler, una respuesta)
            - **INotification** — Eventos de dominio (múltiples handlers)
            - **IPipelineBehavior** — Comportamientos transversales (logging, validación)
            """,
    });
});

// ── SQLite + Entity Framework Core ────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=mediator_demo.db"));

// ── MediatR ───────────────────────────────────────────────────────────────────
// AddMediatR escanea el ensamblado y registra automáticamente:
//   - IRequestHandler<,>
//   - INotificationHandler<>
//   - IPipelineBehavior<,>
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

    // Pipeline behaviors — se ejecutan en orden para cada request
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// ── FluentValidation ──────────────────────────────────────────────────────────
// Registra automáticamente todos los validators del ensamblado
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────

// ── Crear y migrar la base de datos al arrancar ───────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();   // Crea el schema y los datos semilla si no existen
}

// ── Middleware global de excepciones ──────────────────────────────────────────
app.UseMiddleware<ExceptionHandlerMiddleware>();

// ── Swagger UI ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediatR Demo v1");
    c.RoutePrefix   = string.Empty;   // Swagger en la raíz /
    c.DocumentTitle = "MediatR Demo API";
    c.DisplayRequestDuration();
});

app.MapControllers();

// Redirigir / a Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
