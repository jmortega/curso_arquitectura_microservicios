using FluentAssertions;
using Grpc.Core;
using GrpcServer.Data;
using GrpcServer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcServer.Tests.Services;

/// <summary>
/// Tests unitarios del servicio gRPC CatalogoProductosService.
/// No se necesita servidor real — se instancia el servicio directamente
/// y se pasa un FakeServerCallContext.
/// </summary>
public class CatalogoProductosServiceTests
{
    // ─── Instancia del servicio bajo test ──────────────────────
    private static CatalogoProductosService CrearServicio() =>
        new(new ProductoRepository(),
            NullLogger<CatalogoProductosService>.Instance);

    private static readonly FakeServerCallContext Ctx = FakeServerCallContext.Crear();

    // ═══════════════════════════════════════════════════════════
    // ObtenerProducto
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ObtenerProducto_IdExistente_RetornaProductoCorrecto()
    {
        var sut = CrearServicio();

        var resultado = await sut.ObtenerProducto(
            new ObtenerProductoRequest { Id = "1" }, Ctx);

        resultado.Should().NotBeNull();
        resultado.Id.Should().Be("1");
        resultado.Nombre.Should().Be("Laptop Pro 15");
        resultado.Precio.Should().Be(1299.99);
        resultado.Categoria.Should().Be("Electrónica");
    }

    [Fact]
    public async Task ObtenerProducto_IdInexistente_LanzaRpcExceptionNotFound()
    {
        var sut = CrearServicio();

        var accion = () => sut.ObtenerProducto(
            new ObtenerProductoRequest { Id = "999" }, Ctx);

        var ex = await accion.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
        ex.Which.Status.Detail.Should().Contain("999");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    public async Task ObtenerProducto_TodosLosDatosSemilla_Existen(string id)
    {
        var sut = CrearServicio();

        var resultado = await sut.ObtenerProducto(
            new ObtenerProductoRequest { Id = id }, Ctx);

        resultado.Id.Should().Be(id);
        resultado.Nombre.Should().NotBeNullOrEmpty();
        resultado.Precio.Should().BeGreaterThan(0);
    }

    // ═══════════════════════════════════════════════════════════
    // ListarProductos
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ListarProductos_SinFiltros_Devuelve5ProductosSemilla()
    {
        var sut = CrearServicio();

        var resultado = await sut.ListarProductos(
            new ListarProductosRequest { Pagina = 1, Tamanio = 10 }, Ctx);

        resultado.Productos.Should().HaveCount(5);
        resultado.Total.Should().Be(5);
        resultado.Pagina.Should().Be(1);
    }

    [Fact]
    public async Task ListarProductos_FiltroCategoria_DevuelveOnlyElectrónica()
    {
        var sut = CrearServicio();

        var resultado = await sut.ListarProductos(
            new ListarProductosRequest
            {
                Categoria = "Electrónica",
                Pagina    = 1,
                Tamanio   = 10,
            }, Ctx);

        resultado.Productos.Should().HaveCount(2);
        resultado.Productos.Should().AllSatisfy(
            p => p.Categoria.Should().Be("Electrónica"));
    }

    [Fact]
    public async Task ListarProductos_Paginacion_DevuelvePaginaCorrecta()
    {
        var sut = CrearServicio();

        var pagina1 = await sut.ListarProductos(
            new ListarProductosRequest { Pagina = 1, Tamanio = 2 }, Ctx);

        var pagina2 = await sut.ListarProductos(
            new ListarProductosRequest { Pagina = 2, Tamanio = 2 }, Ctx);

        pagina1.Productos.Should().HaveCount(2);
        pagina2.Productos.Should().HaveCount(2);
        pagina1.Productos.Select(p => p.Id)
            .Should().NotIntersectWith(pagina2.Productos.Select(p => p.Id));
    }

    [Fact]
    public async Task ListarProductos_TamanioDefault_Aplicado()
    {
        var sut = CrearServicio();

        // Pagina=0 y Tamanio=0 deben usar valores por defecto (1 y 10)
        var resultado = await sut.ListarProductos(
            new ListarProductosRequest { Pagina = 0, Tamanio = 0 }, Ctx);

        resultado.Pagina.Should().Be(1);
        resultado.Tamanio.Should().Be(10);
    }

    [Fact]
    public async Task ListarProductos_CategoriaInexistente_DevuelveListaVacia()
    {
        var sut = CrearServicio();

        var resultado = await sut.ListarProductos(
            new ListarProductosRequest { Categoria = "NoExiste", Pagina = 1, Tamanio = 10 },
            Ctx);

        resultado.Productos.Should().BeEmpty();
        resultado.Total.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════
    // CrearProducto
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CrearProducto_DatosValidos_RetornaProductoConId()
    {
        var sut = CrearServicio();

        var resultado = await sut.CrearProducto(
            new CrearProductoRequest
            {
                Nombre      = "Webcam HD",
                Descripcion = "Cámara 1080p con micrófono",
                Precio      = 59.99,
                Stock       = 30,
                Categoria   = "Periféricos",
            }, Ctx);

        resultado.Id.Should().NotBeNullOrEmpty();
        resultado.Nombre.Should().Be("Webcam HD");
        resultado.Precio.Should().Be(59.99);
        resultado.CreadoEn.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CrearProducto_SinNombre_LanzaRpcExceptionInvalidArgument()
    {
        var sut = CrearServicio();

        var accion = () => sut.CrearProducto(
            new CrearProductoRequest { Nombre = "", Precio = 10 }, Ctx);

        var ex = await accion.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public async Task CrearProducto_PrecioInvalido_LanzaRpcExceptionInvalidArgument(double precio)
    {
        var sut = CrearServicio();

        var accion = () => sut.CrearProducto(
            new CrearProductoRequest { Nombre = "Test", Precio = precio }, Ctx);

        var ex = await accion.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CrearProducto_ProductoApareceEnListado()
    {
        var sut = CrearServicio();

        var creado = await sut.CrearProducto(
            new CrearProductoRequest
            {
                Nombre    = "SSD 1TB",
                Precio    = 89.99,
                Categoria = "Almacenamiento",
            }, Ctx);

        var listado = await sut.ListarProductos(
            new ListarProductosRequest { Pagina = 1, Tamanio = 100 }, Ctx);

        listado.Productos.Should().Contain(p => p.Id == creado.Id);
    }

    // ═══════════════════════════════════════════════════════════
    // ActualizarProducto
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ActualizarProducto_DatosValidos_RetornaProductoActualizado()
    {
        var sut = CrearServicio();

        var resultado = await sut.ActualizarProducto(
            new ActualizarProductoRequest
            {
                Id          = "1",
                Nombre      = "Laptop Pro 16 (Actualizado)",
                Descripcion = "Nueva descripción",
                Precio      = 1399.99,
                Stock       = 20,
                Categoria   = "Electrónica",
            }, Ctx);

        resultado.Nombre.Should().Be("Laptop Pro 16 (Actualizado)");
        resultado.Precio.Should().Be(1399.99);
        resultado.ActualizadoEn.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ActualizarProducto_IdInexistente_LanzaRpcExceptionNotFound()
    {
        var sut = CrearServicio();

        var accion = () => sut.ActualizarProducto(
            new ActualizarProductoRequest
            {
                Id     = "999",
                Nombre = "No existe",
                Precio = 1.0,
            }, Ctx);

        var ex = await accion.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    // ═══════════════════════════════════════════════════════════
    // EliminarProducto
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task EliminarProducto_IdExistente_RetornaExitoTrue()
    {
        var sut = CrearServicio();

        var resultado = await sut.EliminarProducto(
            new EliminarProductoRequest { Id = "5" }, Ctx);

        resultado.Exito.Should().BeTrue();
        resultado.Mensaje.Should().Contain("5");
    }

    [Fact]
    public async Task EliminarProducto_IdExistente_NoApareceEnListado()
    {
        var sut = CrearServicio();

        await sut.EliminarProducto(
            new EliminarProductoRequest { Id = "3" }, Ctx);

        var listado = await sut.ListarProductos(
            new ListarProductosRequest { Pagina = 1, Tamanio = 100 }, Ctx);

        listado.Productos.Should().NotContain(p => p.Id == "3");
    }

    [Fact]
    public async Task EliminarProducto_IdInexistente_RetornaExitoFalse()
    {
        var sut = CrearServicio();

        var resultado = await sut.EliminarProducto(
            new EliminarProductoRequest { Id = "999" }, Ctx);

        resultado.Exito.Should().BeFalse();
        resultado.Mensaje.Should().Contain("no encontrado");
    }

    // ═══════════════════════════════════════════════════════════
    // Flujo completo CRUD
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task FlujoCRUD_Completo_FuncionaCorrectamente()
    {
        var sut = CrearServicio();

        // 1. Crear
        var creado = await sut.CrearProducto(
            new CrearProductoRequest
            {
                Nombre    = "Producto Test",
                Precio    = 25.00,
                Stock     = 5,
                Categoria = "Test",
            }, Ctx);
        creado.Id.Should().NotBeNullOrEmpty();

        // 2. Obtener
        var obtenido = await sut.ObtenerProducto(
            new ObtenerProductoRequest { Id = creado.Id }, Ctx);
        obtenido.Nombre.Should().Be("Producto Test");

        // 3. Actualizar
        var actualizado = await sut.ActualizarProducto(
            new ActualizarProductoRequest
            {
                Id     = creado.Id,
                Nombre = "Producto Test Editado",
                Precio = 30.00,
                Stock  = 3,
            }, Ctx);
        actualizado.Nombre.Should().Be("Producto Test Editado");
        actualizado.Precio.Should().Be(30.00);

        // 4. Eliminar
        var eliminado = await sut.EliminarProducto(
            new EliminarProductoRequest { Id = creado.Id }, Ctx);
        eliminado.Exito.Should().BeTrue();

        // 5. Verificar que ya no existe
        var accion = () => sut.ObtenerProducto(
            new ObtenerProductoRequest { Id = creado.Id }, Ctx);
        await accion.Should().ThrowAsync<RpcException>()
            .Where(ex => ex.StatusCode == StatusCode.NotFound);
    }
}
