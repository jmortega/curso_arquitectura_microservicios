using CryptoConverter.API.Middleware;
using CryptoConverter.API.Services;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ─── Controladores ─────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "CryptoConverter API",
        Version     = "v1",
        Description = """
            API para convertir euros a criptomonedas en tiempo real.
            Los precios se obtienen desde CoinGecko.

            **Monedas soportadas:** BTC, ETH, BNB, SOL, ADA

            **Endpoints principales:**
            - `GET /api/conversion/precios` — todos los precios
            - `GET /api/conversion/precios/{moneda}` — precio de una moneda
            - `POST /api/conversion/convertir` — conversión con body JSON
            - `GET /api/conversion/convertir?euros=1000&moneda=BTC` — conversión rápida
            """,
        Contact = new OpenApiContact
        {
            Name  = "CryptoConverter Team",
            Email = "api@cryptoconverter.io",
        },
    });

    // Incluir comentarios XML en Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ─── HttpClient con resiliencia (reintentos + circuit breaker) ──────────────
// Solo reintenta errores transitorios (5xx, timeout, red).
// 403/401/400 son errores del cliente → no se reintentan.
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()                      // 5xx + HttpRequestException
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, attempt, _) =>
        {
            Console.WriteLine($"[Polly] Reintento {attempt} en {timespan.TotalSeconds}s. " +
                              $"Error: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
        });

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30));

builder.Services
    .AddHttpClient<ICryptoService, CryptoService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/");
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        // CoinGecko exige un User-Agent descriptivo o devuelve 403
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "CryptoConverter/1.0 (contact@cryptoconverter.io)");
    })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

// ─── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ─── Logging ───────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ────────────────────────────────────────────────────────────────────────────

// ─── Middleware de excepciones globales ─────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

// ─── Swagger UI (siempre visible para facilitar el desarrollo) ──────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoConverter API v1");
    c.RoutePrefix        = string.Empty;   // Swagger en la raíz: http://localhost:5000
    c.DocumentTitle      = "CryptoConverter API";
    c.DisplayRequestDuration();
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Redirigir raíz a Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Necesario para que WebApplicationFactory lo use en los tests de integración
public partial class Program { }
