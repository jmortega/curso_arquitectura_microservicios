using GrpcServer;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ─── Controladores ──────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── Swagger ────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "GrpcClient API",
        Version = "v1",
        Description = """
            API REST que actúa como **cliente gRPC** del servicio `CatalogoProductos`.

            ## Arquitectura
            ```
            Navegador/Postman
                  │  HTTP + JSON
                  ▼
            GrpcClient (este servicio) — Puerto 5100
                  │  gRPC (HTTP/2 + Protobuf)
                  ▼
            GrpcServer — Puerto 5200
            ```

            ## Operaciones disponibles
            - **CRUD** completo de productos
            - **Stream** de actualizaciones de precios en tiempo real
            """,
    });

    // Incluir comentarios XML en Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ─── Cliente gRPC ───────────────────────────────────────────────────────────
// La URL del servidor gRPC se lee desde la configuración
var grpcServerUrl = builder.Configuration["GrpcServer:Url"]
    ?? "http://localhost:5200";

builder.Services
    .AddGrpcClient<CatalogoProductos.CatalogoProductosClient>(options =>
    {
        options.Address = new Uri(grpcServerUrl);
    })
    .ConfigureChannel(channelOptions =>
    {
        // Permitir HTTP sin TLS en desarrollo/Docker
        channelOptions.HttpHandler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout    = TimeSpan.FromMinutes(5),
            KeepAlivePingDelay             = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout           = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true,
        };
    });

// ─── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ─── Logging ────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ────────────────────────────────────────────────────────────────────────────

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GrpcClient API v1");
    c.RoutePrefix   = string.Empty;   // Swagger en la raíz
    c.DocumentTitle = "GrpcClient — Catálogo de Productos";
    c.DisplayRequestDuration();
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Redirigir / a Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
