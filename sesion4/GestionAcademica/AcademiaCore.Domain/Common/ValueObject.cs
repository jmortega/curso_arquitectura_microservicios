// ── Domain/Common/ValueObject.cs ─────────────────────────────────────
namespace AcademiaCore.Domain.Common;

/// <summary>
/// Clase base para Value Objects.
/// Sin identidad propia — su igualdad se basa en sus atributos.
/// Son inmutables por definición.
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return GetEqualityComponents()
            .SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
               .Aggregate(0, HashCode.Combine);

    public static bool operator ==(ValueObject? a, ValueObject? b)
        => a?.Equals(b) ?? b is null;

    public static bool operator !=(ValueObject? a, ValueObject? b)
        => !(a == b);
}