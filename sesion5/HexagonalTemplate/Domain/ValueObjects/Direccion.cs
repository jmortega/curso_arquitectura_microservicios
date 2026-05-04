namespace GestionAcademica.Domain.ValueObjects;

/// <summary>
/// Value Object: objeto inmutable definido por sus atributos, sin identidad propia.
/// Dos Direcciones son iguales si todos sus campos son iguales.
/// </summary>
public sealed class Direccion
{
    public string Calle      { get; }
    public string Ciudad     { get; }
    public string CodigoPostal { get; }
    public string Pais       { get; }

    public string Completa => $"{Calle}, {CodigoPostal} {Ciudad}, {Pais}";

    private Direccion(string calle, string ciudad,
                      string codigoPostal, string pais)
    {
        Calle       = calle;
        Ciudad      = ciudad;
        CodigoPostal = codigoPostal;
        Pais        = pais;
    }

    public static Direccion Crear(string calle, string ciudad,
                                   string codigoPostal, string pais)
    {
        if (string.IsNullOrWhiteSpace(calle))
            throw new ArgumentException("La calle es obligatoria.");

        if (string.IsNullOrWhiteSpace(ciudad))
            throw new ArgumentException("La ciudad es obligatoria.");

        if (string.IsNullOrWhiteSpace(codigoPostal))
            throw new ArgumentException("El código postal es obligatorio.");

        return new Direccion(
            calle.Trim(), ciudad.Trim(),
            codigoPostal.Trim(), pais.Trim());
    }

    // Igualdad por valor — dos objetos con los mismos datos son iguales
    public override bool Equals(object? obj)
    {
        if (obj is not Direccion otra) return false;
        return Calle       == otra.Calle       &&
               Ciudad      == otra.Ciudad      &&
               CodigoPostal == otra.CodigoPostal &&
               Pais        == otra.Pais;
    }

    public override int GetHashCode()
        => HashCode.Combine(Calle, Ciudad, CodigoPostal, Pais);

    public override string ToString() => Completa;
}