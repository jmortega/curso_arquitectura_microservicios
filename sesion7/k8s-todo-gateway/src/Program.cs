using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TodoApi.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Todo API — MySQL + Docker Compose",
        Version = "v1",
        Description = "CRUD de Todos persistido en MySQL. Desplegado con Docker Compose."
    });
});

// ── MySQL vía EF Core (Pomelo) ────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("MySQL")
    ?? throw new InvalidOperationException("Connection string 'MySQL' not found.");

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

// ── Migración automática al arrancar ─────────────────────────────────────────
// Espera a que MySQL esté listo y aplica las migraciones pendientes
await WaitForDatabaseAsync(app);

// ── Swagger UI ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz "/"
});

// ── Endpoints ─────────────────────────────────────────────────────────────────

app.MapGet("/todos", async (TodoDbContext db) =>
    Results.Ok(await db.Todos.ToListAsync()))
    .WithName("GetAllTodos")
    .WithSummary("Listar todos los Todos")
    .WithTags("Todos");

app.MapGet("/todos/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    return todo is null
        ? Results.NotFound(new { error = $"Todo {id} no encontrado" })
        : Results.Ok(todo);
})
    .WithName("GetTodoById")
    .WithSummary("Obtener un Todo por ID")
    .WithTags("Todos");

app.MapPost("/todos", async (CreateTodoRequest req, TodoDbContext db) =>
{
    var todo = new Todo { Title = req.Title, Done = false };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
})
    .WithName("CreateTodo")
    .WithSummary("Crear un nuevo Todo")
    .WithTags("Todos");

app.MapPut("/todos/{id:int}", async (int id, UpdateTodoRequest req, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound(new { error = $"Todo {id} no encontrado" });

    todo.Title = req.Title;
    todo.Done  = req.Done;
    await db.SaveChangesAsync();
    return Results.Ok(todo);
})
    .WithName("UpdateTodo")
    .WithSummary("Actualizar un Todo")
    .WithTags("Todos");

app.MapDelete("/todos/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound(new { error = $"Todo {id} no encontrado" });

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
    .WithName("DeleteTodo")
    .WithSummary("Eliminar un Todo")
    .WithTags("Todos");

app.MapGet("/health", async (TodoDbContext db) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        return Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow });
    }
    catch
    {
        return Results.StatusCode(503);
    }
})
    .WithName("Health")
    .ExcludeFromDescription();

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task WaitForDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<TodoDbContext>>();

    var retries = 10;
    while (retries-- > 0)
    {
        try
        {
            logger.LogInformation("Intentando conectar con MySQL... ({retries} intentos restantes)", retries);
            // EnsureCreatedAsync crea las tablas directamente desde el modelo
            // sin necesitar ficheros de migración
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("MySQL conectado y migraciones aplicadas.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning("MySQL no disponible: {msg}. Reintentando en 3 s...", ex.Message);
            await Task.Delay(3000);
        }
    }
    throw new Exception("No se pudo conectar con MySQL tras varios intentos.");
}

// ── Records ───────────────────────────────────────────────────────────────────
record CreateTodoRequest(string Title);
record UpdateTodoRequest(string Title, bool Done);
