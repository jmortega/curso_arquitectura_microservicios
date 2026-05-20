namespace AcademyManager.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return GetEqualityComponents()
            .SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Select(x => x.GetHashCode())
            .Aggregate((a, b) => a ^ b);

    public static bool operator ==(ValueObject? a, ValueObject? b) =>
        a is null ? b is null : a.Equals(b);

    public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);
}
