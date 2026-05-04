using Testcontainers.MySql;
using Xunit;

namespace MediatRDemo.IntegrationTests.Fixtures;

/// <summary>
/// IAsyncLifetime que levanta un contenedor MySQL real usando Testcontainers.
/// Se comparte entre todos los tests de la misma colección para evitar
/// arrancar/parar MySQL en cada test (costoso).
///
/// Testcontainers descarga la imagen mysql:8.0 y arranca un contenedor
/// real de Docker, garantizando que los tests usan la misma base de datos
/// que producción (vs. un in-memory database que puede comportarse diferente).
/// </summary>
public sealed class MysqlContainerFixture : IAsyncLifetime
{
    // Builder de Testcontainers para MySQL
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .WithDatabase("mediator_test")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    /// <summary>Connection string para el contenedor MySQL del test.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        // Arranca el contenedor Docker (descarga la imagen si no existe)
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        // Para y elimina el contenedor al terminar los tests
        await _container.StopAsync();
    }
}

/// <summary>
/// Colección xUnit que comparte el MysqlContainerFixture entre todos los
/// tests que pertenecen a esta colección.
/// Un único contenedor MySQL para todos los tests = más rápido.
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection
    : ICollectionFixture<MysqlContainerFixture> { }
