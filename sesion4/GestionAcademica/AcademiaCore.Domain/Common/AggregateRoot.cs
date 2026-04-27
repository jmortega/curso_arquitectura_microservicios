// ── Domain/Common/AggregateRoot.cs ───────────────────────────────────
namespace AcademiaCore.Domain.Common;

/// <summary>
/// Raíz de agregado: punto de entrada al agregado.
/// Todos los cambios al agregado pasan por aquí.
/// Acumula eventos de dominio que se despachan tras persistir.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _eventos = [];

    public IReadOnlyList<IDomainEvent> EventosDominio => _eventos.AsReadOnly();

    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }

    protected void AgregarEvento(IDomainEvent evento)
        => _eventos.Add(evento);

    public void LimpiarEventos()
        => _eventos.Clear();
}