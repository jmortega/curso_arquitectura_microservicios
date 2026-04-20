using System.ComponentModel.DataAnnotations;

namespace GrpcClient.Models;

// ──────────────────────────────────────────────────────────────
// Request models — usados en los endpoints Swagger
// ──────────────────────────────────────────────────────────────

public class CrearProductoDto
{
    /// <example>Laptop Ultrabook 14</example>
    [Required]
    public string Nombre { get; set; } = string.Empty;

    /// <example>Portátil ligero con pantalla IPS y 16GB RAM</example>
    public string Descripcion { get; set; } = string.Empty;

    /// <example>999.99</example>
    [Required, Range(0.01, double.MaxValue)]
    public double Precio { get; set; }

    /// <example>30</example>
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    /// <example>Electrónica</example>
    public string Categoria { get; set; } = string.Empty;
}

public class ActualizarProductoDto
{
    /// <example>Laptop Ultrabook 14 Pro</example>
    [Required]
    public string Nombre { get; set; } = string.Empty;

    /// <example>Portátil actualizado con 32GB RAM</example>
    public string Descripcion { get; set; } = string.Empty;

    /// <example>1099.99</example>
    [Required, Range(0.01, double.MaxValue)]
    public double Precio { get; set; }

    /// <example>25</example>
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    /// <example>Electrónica</example>
    public string Categoria { get; set; } = string.Empty;
}

// ──────────────────────────────────────────────────────────────
// Response models — para Swagger y serialización JSON
// ──────────────────────────────────────────────────────────────

public class ProductoDto
{
    public string  Id           { get; set; } = string.Empty;
    public string  Nombre       { get; set; } = string.Empty;
    public string  Descripcion  { get; set; } = string.Empty;
    public double  Precio       { get; set; }
    public int     Stock        { get; set; }
    public string  Categoria    { get; set; } = string.Empty;
    public string  CreadoEn     { get; set; } = string.Empty;
    public string? ActualizadoEn { get; set; }
}

public class ListaProductosDto
{
    public IEnumerable<ProductoDto> Productos { get; set; } = [];
    public int Total   { get; set; }
    public int Pagina  { get; set; }
    public int Tamanio { get; set; }
}

public class EliminarProductoDto
{
    public bool   Exito   { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

public record ErrorDto
{
    public string Codigo  { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
