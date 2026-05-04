using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Students.Application.UseCases;
using Students.Domain.Interfaces;
using Students.Infrastructure.Messaging;
using Students.Infrastructure.Persistence.Repositories;

namespace Students.Infrastructure.Configuration;

public static class ServiceExtensions
{
    public static IServiceCollection AddStudentsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // ── Repositorios ───────────────────────────────────────────────────
        services.AddScoped<IStudentRepository>(_ => new StudentRepository(connectionString));
        services.AddScoped<IUserRepository>(_   => new UserRepository(connectionString));

        // ── RabbitMQ — publicador de eventos ──────────────────────────────
        var rabbitSettings = new RabbitMqSettings();
        configuration.GetSection("RabbitMq").Bind(rabbitSettings);
        services.AddSingleton(rabbitSettings);
        services.AddSingleton<IStudentEventPublisher, RabbitMqStudentEventPublisher>();

        // ── Servicios de aplicación ────────────────────────────────────────
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IAuthService,    AuthService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey   = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey no configurada.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings["Issuer"],
                    ValidAudience            = jwtSettings["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                   Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew                = TimeSpan.Zero,    // sin margen extra de expiración
                };

                // Mostrar detalles del error en desarrollo
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        if (ctx.Exception is SecurityTokenExpiredException)
                            ctx.Response.Headers.Append("Token-Expired", "true");
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Students Microservice",
                Version     = "v1",
                Description = """
                    CRUD de alumnos con autenticación JWT.

                    **Flujo de autenticación:**
                    1. `POST /api/v1/auth/register` — crea un usuario
                    2. `POST /api/v1/auth/login` — obtén el token
                    3. Pulsa **Authorize** 🔓 e introduce: `Bearer {token}`

                    **Roles:**
                    - **Admin** — acceso completo
                    - **Teacher** — GET + POST + PUT
                    - **ReadOnly** — solo GET
                    """,
            });

            // Definición del esquema de seguridad JWT en Swagger
            var securityScheme = new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Description  = "Introduce el token JWT: **Bearer {token}**",
                In           = ParameterLocation.Header,
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                Reference    = new OpenApiReference
                {
                    Id   = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme,
                },
            };

            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() },
            });
        });

        return services;
    }
}
