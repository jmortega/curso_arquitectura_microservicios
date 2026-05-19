namespace AcademyManager.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public TId Id { get; protected set; } = default!;

    protected Entity() { }
    protected Entity(TId id) => Id = id;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
