namespace GestionAcademica.Application.DTOs;

// ── DTOs de Alumno ────────────────────────────────────────────────────

/// <summary>DTO de salida — lo que el sistema devuelve al exterior.</summary>
public record AlumnoDto(
    int    Id,
    string NombreCompleto,
    string Nombre,
    string Apellidos,
    string Email,
    bool   Activo);

/// <summary>DTO de entrada — lo que el exterior envía al sistema.</summary>
public record CrearAlumnoRequest(
    string Nombre,
    string Apellidos,
    string Email);

// ── DTOs de Asignatura ────────────────────────────────────────────────
public record AsignaturaDto(
    int    Id,
    string Codigo,
    string Nombre,
    int    Creditos,
    bool   Activa);

public record CrearAsignaturaRequest(
    string Codigo,
    string Nombre,
    int    Creditos);

// ── DTOs de Matrícula ─────────────────────────────────────────────────
public record MatriculaDto(
    int      Id,
    int      AlumnoId,
    string   NombreAlumno,
    int      AsignaturaId,
    string   NombreAsignatura,
    string   Periodo,
    DateTime FechaAlta,
    bool     Activa);

public record MatricularAlumnoRequest(
    int    AlumnoId,
    int    AsignaturaId,
    string Periodo);