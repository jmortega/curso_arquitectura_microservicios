using Grpc.Core;
using GrpcServer.Data;

namespace GrpcServer.Services;

/// <summary>
/// Implementación del servicio gRPC CatalogoProductos.
/// Gestiona operaciones CRUD sobre el catálogo y streaming de precios.
/// </summary>
public class CatalogoProductosService : CatalogoProductos.CatalogoProductosBase
{
    private readonly ProductoRepository _repo;
    private readonly ILogger<CatalogoProductosService> _logger;

    public CatalogoProductosService(
        ProductoRepository repo,
        ILogger<CatalogoProductosService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────
    // ObtenerProducto
    // ──────────────────────────────────────────────────────────

    public override Task<ProductoResponse> ObtenerProducto(
        ObtenerProductoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("ObtenerProducto: {Id}", request.Id);

        var producto = _repo.ObtenerPorId(request.Id);

        if (producto is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Producto con ID '{request.Id}' no encontrado."));

        return Task.FromResult(MapearRespuesta(producto));
    }

    // ──────────────────────────────────────────────────────────
    // ListarProductos
    // ──────────────────────────────────────────────────────────

    public override Task<ListaProductosResponse> ListarProductos(
        ListarProductosRequest request,
        ServerCallContext context)
    {
        var pagina   = request.Pagina  <= 0 ? 1  : request.Pagina;
        var tamanio  = request.Tamanio <= 0 ? 10 : Math.Min(request.Tamanio, 100);

        _logger.LogInformation(
            "ListarProductos: categoria={Cat} pagina={P} tamanio={T}",
            request.Categoria, pagina, tamanio);

        var productos = _repo.Listar(request.Categoria, pagina, tamanio);
        var total     = _repo.Contar(request.Categoria);

        var respuesta = new ListaProductosResponse
        {
            Total   = total,
            Pagina  = pagina,
            Tamanio = tamanio,
        };
        respuesta.Productos.AddRange(productos.Select(MapearRespuesta));

        return Task.FromResult(respuesta);
    }

    // ──────────────────────────────────────────────────────────
    // CrearProducto
    // ──────────────────────────────────────────────────────────

    public override Task<ProductoResponse> CrearProducto(
        CrearProductoRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "El nombre del producto es obligatorio."));

        if (request.Precio <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "El precio debe ser mayor que 0."));

        _logger.LogInformation("CrearProducto: {Nombre}", request.Nombre);

        var producto = _repo.Crear(
            request.Nombre,
            request.Descripcion,
            request.Precio,
            request.Stock,
            request.Categoria);

        return Task.FromResult(MapearRespuesta(producto));
    }

    // ──────────────────────────────────────────────────────────
    // ActualizarProducto
    // ──────────────────────────────────────────────────────────

    public override Task<ProductoResponse> ActualizarProducto(
        ActualizarProductoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("ActualizarProducto: {Id}", request.Id);

        var producto = _repo.Actualizar(
            request.Id,
            request.Nombre,
            request.Descripcion,
            request.Precio,
            request.Stock,
            request.Categoria);

        if (producto is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Producto con ID '{request.Id}' no encontrado."));

        return Task.FromResult(MapearRespuesta(producto));
    }

    // ──────────────────────────────────────────────────────────
    // EliminarProducto
    // ──────────────────────────────────────────────────────────

    public override Task<EliminarProductoResponse> EliminarProducto(
        EliminarProductoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("EliminarProducto: {Id}", request.Id);

        var eliminado = _repo.Eliminar(request.Id);

        return Task.FromResult(new EliminarProductoResponse
        {
            Exito   = eliminado,
            Mensaje = eliminado
                ? $"Producto '{request.Id}' eliminado correctamente."
                : $"Producto '{request.Id}' no encontrado.",
        });
    }

    // ──────────────────────────────────────────────────────────
    // EscucharPrecios — Server Streaming
    // Envía actualizaciones de precio cada 3 segundos
    // ──────────────────────────────────────────────────────────

    public override async Task EscucharPrecios(
        EscucharPreciosRequest request,
        IServerStreamWriter<PrecioActualizadoResponse> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "EscucharPrecios: {Ids}", string.Join(", ", request.Ids));

        var rng = new Random();
        var iteraciones = 0;

        while (!context.CancellationToken.IsCancellationRequested && iteraciones < 20)
        {
            var productos = _repo.ObtenerPorIds(request.Ids);

            foreach (var producto in productos)
            {
                // Simula variación de precio ±5%
                var variacion   = 1 + (rng.NextDouble() - 0.5) * 0.1;
                var nuevoPrecio = Math.Round(producto.Precio * variacion, 2);

                await responseStream.WriteAsync(new PrecioActualizadoResponse
                {
                    Id        = producto.Id,
                    Nombre    = producto.Nombre,
                    Precio    = nuevoPrecio,
                    Timestamp = DateTime.UtcNow.ToString("O"),
                });
            }

            iteraciones++;
            await Task.Delay(3000, context.CancellationToken);
        }
    }

    // ──────────────────────────────────────────────────────────
    // Mapper
    // ──────────────────────────────────────────────────────────

    private static ProductoResponse MapearRespuesta(Data.Producto p) =>
        new()
        {
            Id           = p.Id,
            Nombre       = p.Nombre,
            Descripcion  = p.Descripcion,
            Precio       = p.Precio,
            Stock        = p.Stock,
            Categoria    = p.Categoria,
            CreadoEn     = p.CreadoEn.ToString("O"),
            ActualizadoEn = p.ActualizadoEn?.ToString("O") ?? string.Empty,
        };
}
