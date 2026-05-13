using Students.Infrastructure.Configuration;
using Students.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger con soporte JWT ────────────────────────────────────────────────
builder.Services.AddSwaggerWithJwt();

// ── Servicios del dominio ──────────────────────────────────────────────────
builder.Services.AddStudentsServices(builder.Configuration);

// ── Autenticación y autorización JWT ──────────────────────────────────────
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Inicialización EAGER del publisher RabbitMQ ───────────────────────────
// Los singletons son lazy por defecto: se crean en la primera petición HTTP.
// Forzamos la creación aquí para que el log de conexión aparezca al arrancar
// y los errores de conexión sean visibles antes de recibir cualquier petición.
_ = app.Services.GetRequiredService<IStudentEventPublisher>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

// El orden es importante: Authentication ANTES que Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
