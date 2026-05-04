using FluentAssertions;
using MediatRDemo.API.Features.Common;
using MediatRDemo.API.Features.Users.Commands;
using MediatRDemo.IntegrationTests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MediatRDemo.IntegrationTests.Pipeline;

/// <summary>
/// Tests que verifican específicamente el comportamiento del Pipeline de MediatR:
/// - ValidationBehavior: rechaza requests inválidas ANTES del handler
/// - LoggingBehavior: no interrumpe el flujo normal
/// - Manejo de errores de dominio → respuestas HTTP correctas
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class MediatRPipelineIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MediatRPipelineIntegrationTests(MysqlContainerFixture mysqlFixture)
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

    // ── ValidationBehavior ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidationBehavior_MultiplesErrores_DevuelveTodosLosErrores()
    {
        // Nombre vacío + email inválido + rol incorrecto → 3 errores de validación
        var command = new CreateUserCommand("", "no-email", "SuperUser");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();

        // La respuesta debe contener los errores de validación
        body.Should().Contain("errors");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidationBehavior_NombreVacio_Intercepta(string nombre)
    {
        var command = new CreateUserCommand(nombre, "valid@test.com", "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("sinArroba")]
    [InlineData("@sindominio")]
    [InlineData("doble@@arroba.com")]
    public async Task ValidationBehavior_EmailInvalido_Intercepta(string email)
    {
        var command = new CreateUserCommand("Nombre Valido", email, "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── ExceptionHandler Middleware ───────────────────────────────────────────

    [Fact]
    public async Task ExceptionHandler_UserNotFound_Devuelve404ConMensaje()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        var json  = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        json.RootElement.GetProperty("message").GetString().Should().Contain("no encontrado");
    }

    [Fact]
    public async Task ExceptionHandler_EmailDuplicado_Devuelve409ConMensaje()
    {
        var command = new CreateUserCommand("Nuevo", "admin@demo.com", "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("admin@demo.com");
    }

    // ── Notificaciones (INotification) ───────────────────────────────────────

    [Fact]
    public async Task Notification_AlCrearUsuario_NoInterrumpeRespuesta()
    {
        // Los 3 handlers de UserCreatedNotification se ejecutan tras el handler principal
        // La respuesta debe ser 201 igualmente (los errores en notificaciones no se propagan al cliente)
        var command = new CreateUserCommand("Notif Test", "notif@test.com", "User");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Email.Should().Be("notif@test.com");
    }

    // ── Content-Type ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/users")]
    public async Task Endpoints_DevuelvenContentTypeJson(string url)
    {
        var response = await _client.GetAsync(url);

        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");
    }
}
