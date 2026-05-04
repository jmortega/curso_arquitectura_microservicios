using FluentAssertions;
using MediatRDemo.API.Features.Common;
using MediatRDemo.IntegrationTests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MediatRDemo.IntegrationTests.Users;

/// <summary>
/// Tests de integración para los endpoints GET de usuarios.
///
/// Cada test:
///   1. Levanta el servidor completo via WebApplicationFactory
///   2. Usa el MySQL real del contenedor Docker (Testcontainers)
///   3. Hace peticiones HTTP reales al API
///   4. Verifica la respuesta
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class GetUsersIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetUsersIntegrationTests(MysqlContainerFixture mysqlFixture)
    {
        _factory = new CustomWebApplicationFactory(mysqlFixture.ConnectionString);
        _client  = _factory.CreateClient();
    }

    public async Task InitializeAsync()
        => await _factory.InitializeDatabaseAsync();

    public async Task DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _factory.Dispose();
    }

    // ── GET /api/v1/users ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_SinFiltros_Devuelve200ConListaDeUsuarios()
    {
        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterOrEqualTo(3);  // datos semilla
    }

    [Fact]
    public async Task GetAll_FiltroOnlyActive_DevuelveSoloUsuariosActivos()
    {
        var response = await _client.GetAsync("/api/v1/users?onlyActive=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetAll_FiltroRolAdmin_DevuelveUsuariosAdmin()
    {
        var response = await _client.GetAsync("/api/v1/users?role=Admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().AllSatisfy(u => u.Role.Should().Be("Admin"));
    }

    // ── GET /api/v1/users/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_IdExistente_Devuelve200ConUsuario()
    {
        var id = Guid.Parse("11111111-0000-0000-0000-000000000001");

        var response = await _client.GetAsync($"/api/v1/users/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(id);
        user.Email.Should().Be("admin@demo.com");
        user.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task GetById_IdInexistente_Devuelve404()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
