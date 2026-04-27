// ── Domain/Common/Entity.cs ───────────────────────────────────────────
namespace AcademiaCore.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// La identidad de una entidad la distingue de otras,
/// incluso si sus atributos son iguales.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    protected Entity() { }

    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
        => a?.Equals(b) ?? b is null;

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b)
        => !(a == b);
}