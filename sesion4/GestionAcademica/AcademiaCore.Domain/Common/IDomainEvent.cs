// ── Domain/Common/IDomainEvent.cs ─────────────────────────────────────
namespace AcademiaCore.Domain.Common;

/// <summary>
/// Marca un objeto como evento de dominio.
/// Los eventos representan algo que ocurrió en el dominio y es relevante.
/// </summary>
public interface IDomainEvent
{
    Guid      EventId   { get; }
    DateTime  OcurridoEn { get; }
}