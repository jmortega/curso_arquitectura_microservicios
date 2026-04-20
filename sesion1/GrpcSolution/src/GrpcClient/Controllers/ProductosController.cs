using Grpc.Core;
using GrpcClient.Models;
using GrpcServer;
using Microsoft.AspNetCore.Mvc;

namespace GrpcClient.Controllers;

/// <summary>
/// API REST que actúa como cliente gRPC del servicio CatalogoProductos.
/// Traduce peticiones HTTP/JSON a llamadas gRPC y devuelve las respuestas como JSON.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController : ControllerBase
{
    private readonly CatalogoProductos.CatalogoProductosClient _grpcClient;
    private readonly ILogger<ProductosController> _logger;

    public ProductosController(
        CatalogoProductos.CatalogoProductosClient grpcClient,
        ILogger<ProductosController> logger)
    {
        _grpcClient = grpcClient;
        _logger     = logger;
    }

    // ──────────────────────────────────────────────────────────
    // GET /api/productos
    // ──────────────────────────────────────────────────────────

    /// <summary>Lista todos los productos con paginación opcional.</summary>
    /// <param name="categoria">Filtrar por categoría (vacío = todas).</param>
    /// <param name="pagina">Número de página (mínimo 1).</param>
    /// <param name="tamanio">Elementos por página (máximo 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(ListaProductosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto),          StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ListarProductos(
        [FromQuery] string categoria = "",
        [FromQuery] int    pagina    = 1,
        [FromQuery] int    tamanio   = 10)
    {
        try
        {
            var respuesta = await _grpcClient.ListarProductosAsync(
                new ListarProductosRequest
                {
                    Categoria = categoria,
                    Pagina    = pagina,
                    Tamanio   = tamanio,
                });

            return Ok(new ListaProductosDto
            {
                Productos = respuesta.Productos.Select(MapearDto),
                Total     = respuesta.Total,
                Pagina    = respuesta.Pagina,
                Tamanio   = respuesta.Tamanio,
            });
        }
        catch (RpcException ex)
        {
            return ManejarErrorGrpc(ex);
        }
    }

    // ──────────────────────────────────────────────────────────
    // GET /api/productos/{id}
    // ──────────────────────────────────────────────────────────

    /// <summary>Obtiene un producto por su ID.</summary>
    /// <param name="id">Identificador único del producto.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto),    StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto),    StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ObtenerProducto([FromRoute] string id)
    {
        try
        {
            var respuesta = await _grpcClient.ObtenerProductoAsync(
                new ObtenerProductoRequest { Id = id });

            return Ok(MapearDto(respuesta));
        }
        catch (RpcException ex)
        {
            return ManejarErrorGrpc(ex);
        }
    }

    // ──────────────────────────────────────────────────────────
    // POST /api/productos
    // ──────────────────────────────────────────────────────────

    /// <summary>Crea un nuevo producto en el catálogo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto),    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto),    StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CrearProducto([FromBody] CrearProductoDto dto)
    {
        try
        {
            var respuesta = await _grpcClient.CrearProductoAsync(
                new CrearProductoRequest
                {
                    Nombre      = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    Precio      = dto.Precio,
                    Stock       = dto.Stock,
                    Categoria   = dto.Categoria,
                });

            var productoCreado = MapearDto(respuesta);
            _logger.LogInformation("Producto creado: {Id} - {Nombre}", respuesta.Id, respuesta.Nombre);

            return CreatedAtAction(
                nameof(ObtenerProducto),
                new { id = respuesta.Id },
                productoCreado);
        }
        catch (RpcException ex)
        {
            return ManejarErrorGrpc(ex);
        }
    }

    // ──────────────────────────────────────────────────────────
    // PUT /api/productos/{id}
    // ──────────────────────────────────────────────────────────

    /// <summary>Actualiza un producto existente.</summary>
    /// <param name="id">Identificador del producto a actualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto),    StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto),    StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ActualizarProducto(
        [FromRoute] string id,
        [FromBody]  ActualizarProductoDto dto)
    {
        try
        {
            var respuesta = await _grpcClient.ActualizarProductoAsync(
                new ActualizarProductoRequest
                {
                    Id          = id,
                    Nombre      = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    Precio      = dto.Precio,
                    Stock       = dto.Stock,
                    Categoria   = dto.Categoria,
                });

            return Ok(MapearDto(respuesta));
        }
        catch (RpcException ex)
        {
            return ManejarErrorGrpc(ex);
        }
    }

    // ──────────────────────────────────────────────────────────
    // DELETE /api/productos/{id}
    // ──────────────────────────────────────────────────────────

    /// <summary>Elimina un producto del catálogo.</summary>
    /// <param name="id">Identificador del producto a eliminar.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(EliminarProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto),            StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> EliminarProducto([FromRoute] string id)
    {
        try
        {
            var respuesta = await _grpcClient.EliminarProductoAsync(
                new EliminarProductoRequest { Id = id });

            return Ok(new EliminarProductoDto
            {
                Exito   = respuesta.Exito,
                Mensaje = respuesta.Mensaje,
            });
        }
        catch (RpcException ex)
        {
            return ManejarErrorGrpc(ex);
        }
    }

    // ──────────────────────────────────────────────────────────
    // GET /api/productos/stream/precios?ids=1&ids=2
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Recibe un stream de actualizaciones de precio durante 60 segundos.
    /// Los precios varían aleatoriamente ±5% cada 3 segundos.
    /// </summary>
    [HttpGet("stream/precios")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EscucharPrecios([FromQuery] List<string> ids)
    {
        var precios = new List<object>();

        try
        {
            using var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var stream  = _grpcClient.EscucharPrecios(
                new EscucharPreciosRequest { Ids = { ids } },
                cancellationToken: cts.Token);

            await foreach (var actualizacion in stream.ResponseStream.ReadAllAsync(cts.Token))
            {
                precios.Add(new
                {
                    actualizacion.Id,
                    actualizacion.Nombre,
                    actualizacion.Precio,
                    actualizacion.Timestamp,
                });
            }
        }
        catch (OperationCanceledException) { /* timeout esperado */ }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { /* ok */ }

        return Ok(precios);
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    private static ProductoDto MapearDto(ProductoResponse p) => new()
    {
        Id            = p.Id,
        Nombre        = p.Nombre,
        Descripcion   = p.Descripcion,
        Precio        = p.Precio,
        Stock         = p.Stock,
        Categoria     = p.Categoria,
        CreadoEn      = p.CreadoEn,
        ActualizadoEn = string.IsNullOrEmpty(p.ActualizadoEn) ? null : p.ActualizadoEn,
    };

    private IActionResult ManejarErrorGrpc(RpcException ex)
    {
        _logger.LogWarning("Error gRPC: {Status} - {Detail}", ex.StatusCode, ex.Status.Detail);

        var error = new ErrorDto { Mensaje = ex.Status.Detail };

        var codigoGrpc = ex.StatusCode;

        return codigoGrpc switch
        {
            Grpc.Core.StatusCode.NotFound        => NotFound(error with { Codigo = "NOT_FOUND" }),
            Grpc.Core.StatusCode.InvalidArgument => BadRequest(error with { Codigo = "INVALID_ARGUMENT" }),
            Grpc.Core.StatusCode.Unavailable     => StatusCode(503, error with { Codigo = "SERVER_UNAVAILABLE" }),
            _                          => StatusCode(500, error with { Codigo = "GRPC_ERROR" }),
        };
    }
}
