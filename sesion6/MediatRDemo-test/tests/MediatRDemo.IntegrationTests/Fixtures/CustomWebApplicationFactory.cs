using MediatRDemo.API.Domain.Entities;
using MediatRDemo.API.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediatRDemo.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory personalizada que:
///   1. Arranca la aplicación completa en memoria (con todos sus middlewares y MediatR)
///   2. Reemplaza el DbContext de producción por uno que apunta al MySQL de Testcontainers
///
/// Esto garantiza que los tests ejercen el stack completo:
///   HTTP → Controller → MediatR Pipeline → Handler → MySQL real (en Docker)
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
        => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Eliminar el DbContext registrado por la aplicación (apuntaba a MySQL de producción)
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Registrar un nuevo DbContext apuntando al MySQL del contenedor de test
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseMySql(
                    _connectionString,
                    ServerVersion.AutoDetect(_connectionString)));
        });

        builder.UseEnvironment("Testing");
    }

    // ── Datos semilla idénticos a los definidos en AppDbContext.OnModelCreating ──
    //
    // Se definen aquí porque EnsureCreated() aplica HasData la PRIMERA vez que
    // crea el schema, pero si la tabla ya existe y la vaciamos manualmente,
    // EnsureCreated() no vuelve a insertarlos.
    // La solución es re-insertarlos explícitamente mediante SeedTestDataAsync().
    //
    private static readonly (Guid Id, string Name, string Email, string Role, bool IsActive)[] SeedUsers =
    [
        (Guid.Parse("11111111-0000-0000-0000-000000000001"), "Admin User",   "admin@demo.com",    "Admin",    true),
        (Guid.Parse("22222222-0000-0000-0000-000000000002"), "Regular User", "user@demo.com",     "User",     true),
        (Guid.Parse("33333333-0000-0000-0000-000000000003"), "Read Only",    "readonly@demo.com", "ReadOnly", false),
    ];

    /// <summary>
    /// Crea el schema y asegura que los datos semilla existen.
    /// Llamar una vez antes de ejecutar los tests de cada clase.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Crea el schema si no existe (incluye HasData la primera vez)
        await db.Database.EnsureCreatedAsync();

        // Garantizar que los usuarios semilla existen aunque la tabla ya estuviera creada
        await SeedTestDataAsync(db);
    }

    /// <summary>
    /// Limpia todos los usuarios e inserta de nuevo los datos semilla.
    /// Llamar en DisposeAsync de cada clase de test para dejar la BD limpia.
    ///
    /// IMPORTANTE: no se puede usar EnsureCreated() para re-sembrar porque
    /// EF Core solo aplica HasData cuando CREA el schema por primera vez.
    /// Si la tabla ya existe, HasData no vuelve a ejecutarse.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Borrar todos los usuarios (incluidos los creados durante el test)
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        // Reinsertar los datos semilla explícitamente
        await SeedTestDataAsync(db);
    }

    // ── Inserción explícita de datos semilla ──────────────────────────────────

    private static async Task SeedTestDataAsync(AppDbContext db)
    {
        foreach (var (id, name, email, role, isActive) in SeedUsers)
        {
            // Insertar solo si no existe ya (idempotente)
            if (!await db.Users.AnyAsync(u => u.Id == id))
            {
                // Construir la entidad mediante SQL directo para saltarse el
                // constructor privado y respetar los IDs fijos del seed
                await db.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO Users (Id, Name, Email, Role, IsActive, CreatedAt, UpdatedAt)
                    VALUES ({0}, {1}, {2}, {3}, {4}, {5}, NULL)",
                    id.ToString(),
                    name,
                    email,
                    role,
                    isActive ? 1 : 0,
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
            }
        }
    }
}
