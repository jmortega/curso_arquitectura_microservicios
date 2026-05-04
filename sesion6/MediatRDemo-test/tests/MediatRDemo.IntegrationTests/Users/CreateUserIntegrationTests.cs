using FluentAssertions;
using MediatRDemo.API.Controllers;
using MediatRDemo.API.Features.Common;
using MediatRDemo.API.Features.Users.Commands;
using MediatRDemo.IntegrationTests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MediatRDemo.IntegrationTests.Users;

/// <summary>
/// Tests de integración para Commands (POST, PUT, DELETE, PATCH).
/// Verifican que el pipeline completo MediatR funciona correctamente:
/// Controller → LoggingBehavior → ValidationBehavior → Handler → MySQL
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CreateUserIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CreateUserIntegrationTests(MysqlContainerFixture mysqlFixture)
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

    // ── POST /api/v1/users ───────────────────────────────────────────────────

    [Fact]
    public async Task Post_DatosValidos_Devuelve201ConUsuarioCreado()
    {
        var command = new CreateUserCommand("Carlos García", "carlos@test.com", "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Name.Should().Be("Carlos García");
        user.Email.Should().Be("carlos@test.com");
        user.IsActive.Should().BeTrue();

        // Verificar que Location header apunta al nuevo recurso
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(user.Id.ToString());
    }

    [Fact]
    public async Task Post_EmailDuplicado_Devuelve409()
    {
        // Usar email de los datos semilla
        var command = new CreateUserCommand("Duplicado", "admin@demo.com", "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_NombreVacio_Devuelve400PorValidacion()
    {
        // ValidationBehavior intercepta antes de llegar al Handler
        var command = new CreateUserCommand("", "valido@test.com", "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmailInvalido_Devuelve400PorValidacion()
    {
        var command = new CreateUserCommand("Nombre Válido", "no-es-un-email", "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_RolInvalido_Devuelve400PorValidacion()
    {
        var command = new CreateUserCommand("Nombre", "ok@test.com", "SuperAdmin");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/v1/users/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task Put_DatosValidos_Devuelve200ConUsuarioActualizado()
    {
        var id   = Guid.Parse("22222222-0000-0000-0000-000000000002");
        var body = new UpdateUserBody("Nombre Actualizado", "actualizado@test.com", "Admin");

        var response = await _client.PutAsJsonAsync($"/api/v1/users/{id}", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Name.Should().Be("Nombre Actualizado");
        user.Role.Should().Be("Admin");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Put_IdInexistente_Devuelve404()
    {
        var body = new UpdateUserBody("Nombre", "email@test.com", "User");

        var response = await _client.PutAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH activate / deactivate ──────────────────────────────────────────

    [Fact]
    public async Task Patch_Deactivate_Devuelve200ConIsActiveFalse()
    {
        var id = Guid.Parse("11111111-0000-0000-0000-000000000001");

        var response = await _client.PatchAsync($"/api/v1/users/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("false");
    }

    [Fact]
    public async Task Patch_Activate_Devuelve200ConIsActiveTrue()
    {
        // El usuario 33333333 está desactivado en los datos semilla
        var id = Guid.Parse("33333333-0000-0000-0000-000000000003");

        var response = await _client.PatchAsync($"/api/v1/users/{id}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("true");
    }

    // ── DELETE /api/v1/users/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Delete_IdExistente_Devuelve204YNoApareceEnListado()
    {
        // Crear primero un usuario para poder borrarlo sin romper otros tests
        var created = await _client.PostAsJsonAsync("/api/v1/users",
            new CreateUserCommand("Para Borrar", "borrar@test.com", "ReadOnly"));
        var user = await created.Content.ReadFromJsonAsync<UserDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/users/{user!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que ya no existe
        var getResponse = await _client.GetAsync($"/api/v1/users/{user.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_IdInexistente_Devuelve404()
    {
        var response = await _client.DeleteAsync($"/api/v1/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Flujo completo CRUD ──────────────────────────────────────────────────

    [Fact]
    public async Task FlujoCRUD_CrearActualizarEliminar_FuncionaCorrectamente()
    {
        // 1. Crear
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users",
            new CreateUserCommand("CRUD Test", "crud@test.com", "User"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // 2. Obtener por ID
        var getResponse = await _client.GetAsync($"/api/v1/users/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Actualizar
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/users/{created.Id}",
            new UpdateUserBody("CRUD Actualizado", "crud_updated@test.com", "Admin"));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<UserDto>();
        updated!.Name.Should().Be("CRUD Actualizado");

        // 4. Desactivar
        var deactivateResponse = await _client.PatchAsync(
            $"/api/v1/users/{created.Id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verificar que no aparece en filtro de activos
        var activeUsers = await _client.GetFromJsonAsync<List<UserDto>>(
            "/api/v1/users?onlyActive=true");
        activeUsers!.Should().NotContain(u => u.Id == created.Id);

        // 6. Eliminar
        var deleteResponse = await _client.DeleteAsync($"/api/v1/users/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
