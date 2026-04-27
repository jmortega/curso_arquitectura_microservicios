// ── Domain/Aggregates/Matricula.cs ────────────────────────────────────
namespace AcademiaCore.Domain.Aggregates;

using AcademiaCore.Domain.Common;
using AcademiaCore.Domain.Entities;
using AcademiaCore.Domain.Events;
using AcademiaCore.Domain.Exceptions;
using AcademiaCore.Domain.Matriculacion.Events;
using AcademiaCore.Domain.Matriculacion.ValueObjects;

public class Matricula : AggregateRoot<Guid>
{
    private readonly List<LineaMatricula> _lineas = [];

    public int              EstudianteId     { get; private set; }
    public PeriodoAcademico Periodo          { get; private set; } = null!;
    public EstadoMatricula  Estado           { get; private set; }
    public DateTime         FechaCreacion    { get; private set; }
    public DateTime?        FechaCancelacion { get; private set; }
    public string?          MotivoCancelacion { get; private set; }

    public IReadOnlyList<LineaMatricula> Lineas => _lineas.AsReadOnly();
    public int  TotalCreditos => _lineas.Sum(l => l.Creditos);
    public bool EstaActiva    => Estado == EstadoMatricula.Activa;

    private const int MaxCreditos = 30;
    private const int MinCreditos = 12;

    private Matricula() { }

    // ── Factory method ─────────────────────────────────────────────────
    public static Matricula Crear(int estudianteId,
                                  PeriodoAcademico periodo,
                                  List<Asignatura> asignaturas)
    {
        if (asignaturas is null || asignaturas.Count == 0)
            throw new MatriculacionException(
                "Debe seleccionar al menos una asignatura.");

        var matricula = new Matricula
        {
            Id            = Guid.NewGuid(),
            EstudianteId  = estudianteId,
            Periodo       = periodo,
            Estado        = EstadoMatricula.Activa,
            FechaCreacion = DateTime.UtcNow
        };

        foreach (var asignatura in asignaturas)
            matricula.AgregarLinea(asignatura);

        if (matricula.TotalCreditos < MinCreditos)
            throw new MatriculacionException(
                $"La matrícula debe tener al menos {MinCreditos} créditos. " +
                $"Actuales: {matricula.TotalCreditos}.");

        matricula.AgregarEvento(new MatriculaCreada(
            matricula.Id,
            estudianteId,
            periodo.Codigo,
            asignaturas.Select(a => a.Nombre).ToList(),
            matricula.TotalCreditos));

        return matricula;
    }

    // ── Comportamiento del agregado ────────────────────────────────────
    private void AgregarLinea(Asignatura asignatura)
    {
        if (_lineas.Any(l => l.AsignaturaId == asignatura.Id))
            throw new MatriculacionException(
                $"'{asignatura.Nombre}' ya está en la matrícula.");

        if (TotalCreditos + asignatura.Creditos > MaxCreditos)
            throw new MatriculacionException(
                $"Añadir '{asignatura.Nombre}' superaría el límite " +
                $"de {MaxCreditos} créditos. Actuales: {TotalCreditos}.");

        asignatura.OcuparPlaza();

        _lineas.Add(new LineaMatricula(
            asignatura.Id,
            asignatura.Nombre,
            asignatura.Creditos));
    }

    public void Cancelar(string motivo, List<Asignatura> asignaturas)
    {
        if (!EstaActiva)
            throw new MatriculacionException(
                $"No se puede cancelar una matrícula en estado '{Estado}'.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de cancelación es obligatorio.");

        foreach (var asignatura in asignaturas)
            asignatura.LiberarPlaza();

        Estado            = EstadoMatricula.Cancelada;
        FechaCancelacion  = DateTime.UtcNow;
        MotivoCancelacion = motivo.Trim();

        AgregarEvento(new MatriculaCancelada(
            Id, EstudianteId, Periodo.Codigo, motivo));
    }
}

public class LineaMatricula
{
    // Clave primaria generada por EF — no se asigna manualmente
    public int Id { get; private set; }

    public int    AsignaturaId     { get; private set; }
    public string NombreAsignatura { get; private set; } = string.Empty;
    public int    Creditos         { get; private set; }

    // Constructor sin parámetros requerido por EF Core
    private LineaMatricula() { }

    // Constructor interno — solo el agregado puede crear líneas
    internal LineaMatricula(int asignaturaId, string nombreAsignatura, int creditos)
    {
        AsignaturaId     = asignaturaId;
        NombreAsignatura = nombreAsignatura;
        Creditos         = creditos;
    }
}

public enum EstadoMatricula { Activa, Cancelada, Cerrada }