using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Todo API — K8s Hello World",
        Version = "v1",
        Description = "Lista de Todos en memoria. Ejemplo de despliegue en Kubernetes."
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz "/"
});

// ── In-memory store ───────────────────────────────────────────────────────────
var todos = new List<Todo>
{
    new(1, "Aprender Kubernetes", false),
    new(2, "Desplegar en Minikube", false),
    new(3, "Dominar kubectl", false)
};
var nextId = 4;

// ── Endpoints ─────────────────────────────────────────────────────────────────

app.MapGet("/todos", () => Results.Ok(todos))
    .WithName("GetAllTodos")
    .WithSummary("Obtener todos los Todos")
    .WithTags("Todos");

app.MapGet("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is null ? Results.NotFound(new { error = $"Todo {id} no encontrado" }) : Results.Ok(todo);
})
    .WithName("GetTodoById")
    .WithSummary("Obtener un Todo por ID")
    .WithTags("Todos");

app.MapPost("/todos", (CreateTodoRequest req) =>
{
    var todo = new Todo(nextId++, req.Title, false);
    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
})
    .WithName("CreateTodo")
    .WithSummary("Crear un Todo")
    .WithTags("Todos");

app.MapPut("/todos/{id:int}", (int id, UpdateTodoRequest req) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    if (todo is null) return Results.NotFound(new { error = $"Todo {id} no encontrado" });
    var updated = todo with { Title = req.Title, Done = req.Done };
    todos[todos.IndexOf(todo)] = updated;
    return Results.Ok(updated);
})
    .WithName("UpdateTodo")
    .WithSummary("Actualizar un Todo")
    .WithTags("Todos");

app.MapDelete("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    if (todo is null) return Results.NotFound(new { error = $"Todo {id} no encontrado" });
    todos.Remove(todo);
    return Results.NoContent();
})
    .WithName("DeleteTodo")
    .WithSummary("Eliminar un Todo")
    .WithTags("Todos");

// Health check básico para los probes de K8s
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithSummary("Health check")
    .ExcludeFromDescription();

app.Run();

// ── Modelos ───────────────────────────────────────────────────────────────────
record Todo(int Id, string Title, bool Done);
record CreateTodoRequest(string Title);
record UpdateTodoRequest(string Title, bool Done);
