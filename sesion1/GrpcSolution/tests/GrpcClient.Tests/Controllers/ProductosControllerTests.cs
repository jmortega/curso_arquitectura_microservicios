using FluentAssertions;
using Grpc.Core;
using GrpcClient.Models;
using GrpcServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GrpcClient.Tests.Controllers;

// ──────────────────────────────────────────────────────────────
// Factory que reemplaza el cliente gRPC real por un Mock de Moq
// ──────────────────────────────────────────────────────────────
public class ClienteTestFactory : WebApplicationFactory<Program>
{
    public Mock<CatalogoProductos.CatalogoProductosClient> GrpcMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<CatalogoProductos.CatalogoProductosClient>();
            services.AddSingleton(GrpcMock.Object);
        });
    }
}

/// <summary>
/// Tests de integración del ProductosController.
/// El cliente gRPC está mockeado con Moq — no se necesita servidor gRPC real.
/// </summary>
public class ProductosControllerTests : IClassFixture<ClienteTestFactory>
{
    private readonly HttpClient _http;
    private readonly Mock<CatalogoProductos.CatalogoProductosClient> _grpcMock;

    // ─── Datos de prueba ────────────────────────────────────────
    private static readonly ProductoResponse ProductoFake = new()
    {
        Id          = "1",
        Nombre      = "Laptop Pro 15",
        Descripcion = "Portátil de alto rendimiento",
        Precio      = 1299.99,
        Stock       = 25,
        Categoria   = "Electrónica",
        CreadoEn    = DateTime.UtcNow.ToString("O"),
    };

    public ProductosControllerTests(ClienteTestFactory factory)
    {
        _http     = factory.CreateClient();
        _grpcMock = factory.GrpcMock;
        _grpcMock.Reset();
    }

    // ═══════════════════════════════════════════════════════════
    // GET /api/productos
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GET_Productos_Devuelve200ConListado()
    {
        var listaFake = new ListaProductosResponse { Total = 1, Pagina = 1, Tamanio = 10 };
        listaFake.Productos.Add(ProductoFake);

        _grpcMock
            .Setup(c => c.ListarProductosAsync(
                It.IsAny<ListarProductosRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new Grpc.Core.AsyncUnaryCall<ListaProductosResponse>(
                Task.FromResult(listaFake),
                Task.FromResult(new Grpc.Core.Metadata()),
                () => new Status(StatusCode.OK, ""),
                () => new Grpc.Core.Metadata(),
                () => { }));

        var response = await _http.GetAsync("/api/productos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ListaProductosDto>();
        dto!.Total.Should().Be(1);
        dto.Productos.Should().HaveCount(1);
    }

    [Fact]
    public async Task GET_Productos_ServerGrpcNoDisponible_Devuelve503()
    {
        _grpcMock
            .Setup(c => c.ListarProductosAsync(
                It.IsAny<ListarProductosRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "Server down")));

        var response = await _http.GetAsync("/api/productos");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    // ═══════════════════════════════════════════════════════════
    // GET /api/productos/{id}
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GET_ProductoPorId_IdExistente_Devuelve200()
    {
        _grpcMock
            .Setup(c => c.ObtenerProductoAsync(
                It.Is<ObtenerProductoRequest>(r => r.Id == "1"),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new Grpc.Core.AsyncUnaryCall<ProductoResponse>(
                Task.FromResult(ProductoFake),
                Task.FromResult(new Grpc.Core.Metadata()),
                () => new Status(StatusCode.OK, ""),
                () => new Grpc.Core.Metadata(),
                () => { }));

        var response = await _http.GetAsync("/api/productos/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ProductoDto>();
        dto!.Id.Should().Be("1");
        dto.Nombre.Should().Be("Laptop Pro 15");
    }

    [Fact]
    public async Task GET_ProductoPorId_IdInexistente_Devuelve404()
    {
        _grpcMock
            .Setup(c => c.ObtenerProductoAsync(
                It.IsAny<ObtenerProductoRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.NotFound, "No encontrado")));

        var response = await _http.GetAsync("/api/productos/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        error!.Codigo.Should().Be("NOT_FOUND");
    }

    // ═══════════════════════════════════════════════════════════
    // POST /api/productos
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task POST_CrearProducto_DatosValidos_Devuelve201()
    {
        var nuevoProducto = new ProductoResponse
        {
            Id       = "6",
            Nombre   = "Webcam HD",
            Precio   = 59.99,
            CreadoEn = DateTime.UtcNow.ToString("O"),
        };

        _grpcMock
            .Setup(c => c.CrearProductoAsync(
                It.IsAny<CrearProductoRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new Grpc.Core.AsyncUnaryCall<ProductoResponse>(
                Task.FromResult(nuevoProducto),
                Task.FromResult(new Grpc.Core.Metadata()),
                () => new Status(StatusCode.OK, ""),
                () => new Grpc.Core.Metadata(),
                () => { }));

        var dto = new CrearProductoDto
        {
            Nombre    = "Webcam HD",
            Precio    = 59.99,
            Stock     = 20,
            Categoria = "Periféricos",
        };

        var response = await _http.PostAsJsonAsync("/api/productos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var creado = await response.Content.ReadFromJsonAsync<ProductoDto>();
        creado!.Id.Should().Be("6");
    }

    [Fact]
    public async Task POST_CrearProducto_ArgumentoInvalido_Devuelve400()
    {
        _grpcMock
            .Setup(c => c.CrearProductoAsync(
                It.IsAny<CrearProductoRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.InvalidArgument, "Precio inválido")));

        var dto = new CrearProductoDto { Nombre = "X", Precio = -1 };
        var response = await _http.PostAsJsonAsync("/api/productos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════════════════════
    // PUT /api/productos/{id}
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task PUT_ActualizarProducto_Devuelve200()
    {
        var actualizado = ProductoFake.Clone();
        actualizado.Nombre       = "Laptop Pro 16";
        actualizado.Precio       = 1399.99;
        actualizado.ActualizadoEn = DateTime.UtcNow.ToString("O");

        _grpcMock
            .Setup(c => c.ActualizarProductoAsync(
                It.IsAny<ActualizarProductoRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new Grpc.Core.AsyncUnaryCall<ProductoResponse>(
                Task.FromResult(actualizado),
                Task.FromResult(new Grpc.Core.Metadata()),
                () => new Status(StatusCode.OK, ""),
                () => new Grpc.Core.Metadata(),
                () => { }));

        var dto = new ActualizarProductoDto
        {
            Nombre    = "Laptop Pro 16",
            Precio    = 1399.99,
            Stock     = 20,
            Categoria = "Electrónica",
        };

        var response = await _http.PutAsJsonAsync("/api/productos/1", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ProductoDto>();
        resultado!.Nombre.Should().Be("Laptop Pro 16");
        resultado.Precio.Should().Be(1399.99);
        resultado.ActualizadoEn.Should().NotBeNullOrEmpty();
    }

    // ═══════════════════════════════════════════════════════════
    // DELETE /api/productos/{id}
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task DELETE_EliminarProducto_Devuelve200ConExitoTrue()
    {
        _grpcMock
            .Setup(c => c.EliminarProductoAsync(
                It.IsAny<EliminarProductoRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new Grpc.Core.AsyncUnaryCall<EliminarProductoResponse>(
                Task.FromResult(new EliminarProductoResponse
                {
                    Exito   = true,
                    Mensaje = "Producto '1' eliminado correctamente.",
                }),
                Task.FromResult(new Grpc.Core.Metadata()),
                () => new Status(StatusCode.OK, ""),
                () => new Grpc.Core.Metadata(),
                () => { }));

        var response = await _http.DeleteAsync("/api/productos/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<EliminarProductoDto>();
        resultado!.Exito.Should().BeTrue();
        resultado.Mensaje.Should().Contain("eliminado");
    }

    // ═══════════════════════════════════════════════════════════
    // Content-Type
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData("/api/productos")]
    public async Task Endpoints_DevuelvenContentTypeJson(string url)
    {
        var listaFake = new ListaProductosResponse { Total = 0, Pagina = 1, Tamanio = 10 };

        _grpcMock
            .Setup(c => c.ListarProductosAsync(
                It.IsAny<ListarProductosRequest>(),
                It.IsAny<Grpc.Core.Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new Grpc.Core.AsyncUnaryCall<ListaProductosResponse>(
                Task.FromResult(listaFake),
                Task.FromResult(new Grpc.Core.Metadata()),
                () => new Status(StatusCode.OK, ""),
                () => new Grpc.Core.Metadata(),
                () => { }));

        var response = await _http.GetAsync(url);

        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");
    }
}
