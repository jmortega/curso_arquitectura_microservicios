using Students.Infrastructure.Configuration;

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
