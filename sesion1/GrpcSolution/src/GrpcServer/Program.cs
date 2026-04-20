using GrpcServer.Data;
using GrpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── gRPC ──────────────────────────────────────────────────────────────────
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;   // errores detallados (solo dev)
    options.MaxReceiveMessageSize = 4 * 1024 * 1024;  // 4 MB
    options.MaxSendMessageSize    = 4 * 1024 * 1024;
});

// Reflection: permite usar herramientas como grpcurl y Postman
builder.Services.AddGrpcReflection();

// ─── Repositorio en memoria (singleton = datos compartidos) ────────────────
builder.Services.AddSingleton<ProductoRepository>();

// ─── Logging ───────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ────────────────────────────────────────────────────────────────────────────

// Registrar el servicio gRPC
app.MapGrpcService<CatalogoProductosService>();

// Reflection (útil en desarrollo)
if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

// Endpoint informativo en HTTP
app.MapGet("/", () =>
    "GrpcServer en ejecución. Usa un cliente gRPC para interactuar.");

app.Run();

public partial class Program { }
