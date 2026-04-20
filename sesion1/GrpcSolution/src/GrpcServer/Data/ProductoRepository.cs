namespace GrpcServer.Data;

/// <summary>
/// Repositorio en memoria que actúa como base de datos para el ejemplo.
/// En producción se reemplazaría por EF Core u otro ORM.
/// </summary>
public class ProductoRepository
{
    private readonly List<Producto> _productos;
    private int _nextId = 6;

    public ProductoRepository()
    {
        _productos = new List<Producto>
        {
            new() { Id = "1", Nombre = "Laptop Pro 15",       Descripcion = "Portátil de alto rendimiento",       Precio = 1299.99, Stock = 25, Categoria = "Electrónica",   CreadoEn = DateTime.UtcNow.AddDays(-30) },
            new() { Id = "2", Nombre = "Teclado Mecánico",    Descripcion = "Teclado con switches Cherry MX Red",  Precio = 89.99,  Stock = 50, Categoria = "Periféricos",   CreadoEn = DateTime.UtcNow.AddDays(-20) },
            new() { Id = "3", Nombre = "Monitor 4K 27\"",     Descripcion = "Monitor UHD con HDR10",               Precio = 449.99, Stock = 15, Categoria = "Electrónica",   CreadoEn = DateTime.UtcNow.AddDays(-15) },
            new() { Id = "4", Nombre = "Ratón Inalámbrico",   Descripcion = "Ratón ergonómico sin cables",         Precio = 45.99,  Stock = 80, Categoria = "Periféricos",   CreadoEn = DateTime.UtcNow.AddDays(-10) },
            new() { Id = "5", Nombre = "Auriculares Gaming",  Descripcion = "Sonido envolvente 7.1 virtual",       Precio = 79.99,  Stock = 40, Categoria = "Audio",         CreadoEn = DateTime.UtcNow.AddDays(-5)  },
        };
    }

    public Producto? ObtenerPorId(string id) =>
        _productos.FirstOrDefault(p => p.Id == id);

    public List<Producto> Listar(string categoria, int pagina, int tamanio)
    {
        var query = _productos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(p => p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));

        return query
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .ToList();
    }

    public int Contar(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
            return _productos.Count;

        return _productos.Count(p =>
            p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
    }

    public Producto Crear(string nombre, string descripcion, double precio, int stock, string categoria)
    {
        var producto = new Producto
        {
            Id          = (_nextId++).ToString(),
            Nombre      = nombre,
            Descripcion = descripcion,
            Precio      = precio,
            Stock       = stock,
            Categoria   = categoria,
            CreadoEn    = DateTime.UtcNow,
        };
        _productos.Add(producto);
        return producto;
    }

    public Producto? Actualizar(string id, string nombre, string descripcion, double precio, int stock, string categoria)
    {
        var producto = ObtenerPorId(id);
        if (producto is null) return null;

        producto.Nombre        = nombre;
        producto.Descripcion   = descripcion;
        producto.Precio        = precio;
        producto.Stock         = stock;
        producto.Categoria     = categoria;
        producto.ActualizadoEn = DateTime.UtcNow;
        return producto;
    }

    public bool Eliminar(string id)
    {
        var producto = ObtenerPorId(id);
        if (producto is null) return false;
        _productos.Remove(producto);
        return true;
    }

    public List<Producto> ObtenerPorIds(IEnumerable<string> ids) =>
        _productos.Where(p => ids.Contains(p.Id)).ToList();
}

public class Producto
{
    public string   Id           { get; set; } = string.Empty;
    public string   Nombre       { get; set; } = string.Empty;
    public string   Descripcion  { get; set; } = string.Empty;
    public double   Precio       { get; set; }
    public int      Stock        { get; set; }
    public string   Categoria    { get; set; } = string.Empty;
    public DateTime CreadoEn     { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadoEn { get; set; }
}
