using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] GW | {Message:lj}{NewLine}{Exception}"));

// ── Forwarded Headers ─────────────────────────────────────────────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── YARP: API Gateway ─────────────────────────────────────────────────────────
//
//  YARP se configura desde appsettings.json (sección ReverseProxy).
//  Soporta:
//    - Enrutamiento por path, método HTTP y cabeceras
//    - Balanceo de carga entre réplicas del microservicio
//    - Transformaciones de cabeceras (añadir X-Gateway, propagar X-Correlation-Id)
//    - Health checks activos sobre los microservicios destino
//
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── Prometheus ────────────────────────────────────────────────────────────────
builder.Services.UseHttpClientMetrics();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSerilogRequestLogging();

app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", _ => "academy-gateway");
});

app.MapReverseProxy();
app.MapMetrics("/metrics");

app.Run();
