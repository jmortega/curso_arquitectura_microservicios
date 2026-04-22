using System.Data;
using Dapper;
using DapperAsignaturas;

namespace DapperAsignaturas.Repositories;

public class AsignaturaRepository : IAsignaturaRepository
{
    private readonly IDbConnection _connection;

    public AsignaturaRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    // ─────────────────────────────────────
    // CONSULTAS
    // ─────────────────────────────────────

    public async Task<IEnumerable<Asignatura>> ObtenerTodosAsync()
    {
        string sql = @"SELECT Id, Nombre, Descripcion, Curso, FechaAlta
                       FROM Asignaturas
                       ORDER BY Nombre";

        return await _connection.QueryAsync<Asignatura>(sql);
    }

    public async Task<IEnumerable<Asignatura>> ObtenerTodos()
    {
        string sql = @"SELECT Id, Nombre, Descripcion, Curso, FechaAlta
                       FROM Asignaturas
                       ORDER BY Nombre";

        return _connection.Query<Asignatura>(sql,buffered: false);
    }

    public async Task<IEnumerable<dynamic>> ObtenerTodosAsyncDynamic()
{
    string sql = @"SELECT Id, Nombre, Descripcion, Curso, FechaAlta
                   FROM Asignaturas
                   ORDER BY Nombre";

    return await _connection.QueryAsync(sql);
}

    public async Task<Asignatura?> ObtenerPorIdAsync(int id)
    {
        string sql = @"SELECT Id, Nombre, Descripcion, Curso, FechaAlta
                       FROM Asignaturas
                       WHERE Id = @Id";

        return await _connection.QueryFirstOrDefaultAsync<Asignatura>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Asignatura>> BuscarPorNombreAsync(string nombre)
    {
        string sql = @"SELECT Id, Nombre, Descripcion, Curso, FechaAlta
                       FROM Asignaturas
                       WHERE LOWER(Nombre) LIKE LOWER(@Nombre)
                       ORDER BY Nombre";

        return await _connection.QueryAsync<Asignatura>(sql, new { Nombre = $"%{nombre}%" });
    }

    public async Task<IEnumerable<Asignatura>> ObtenerPorCursoAsync(string curso)
    {
        string sql = @"SELECT Id, Nombre, Descripcion, Curso, FechaAlta
                       FROM Asignaturas
                       WHERE LOWER(Curso) = LOWER(@Curso)
                       ORDER BY Nombre";

        return await _connection.QueryAsync<Asignatura>(sql, new { Curso = curso });
    }

    public async Task<int> ObtenerTotalAsignaturasAsync()
    {
        string sql = "SELECT COUNT(*) FROM Asignaturas";
        return await _connection.ExecuteScalarAsync<int>(sql);
    }

    // ─────────────────────────────────────
    // MATRICULACIONES — CONSULTA
    // ─────────────────────────────────────

    /// <summary>
    /// Obtiene todas las matriculaciones de un alumno junto con
    /// los datos de la asignatura usando multi-mapping (JOIN).
    /// </summary>
    public async Task<IEnumerable<Matriculacion>> ObtenerMatriculacionesPorAlumnoAsync(int alumnoId)
    {
        string sql = @"
            SELECT m.Id,
                   m.AlumnoId,
                   m.AsignaturaId,
                   m.FechaAlta,
                   m.Estado,
                   m.Nota,
                   a.Id,
                   a.Nombre,
                   a.Descripcion,
                   a.Curso,
                   a.FechaAlta
            FROM Matriculaciones m
            INNER JOIN Asignaturas a ON a.Id = m.AsignaturaId
            WHERE m.AlumnoId = @AlumnoId
            ORDER BY a.Nombre";

        // splitOn: la segunda columna 'Id' indica donde empieza el objeto Asignatura
        return await _connection.QueryAsync<Matriculacion, Asignatura, Matriculacion>(
            sql,
            map: (matriculacion, asignatura) =>
            {
                matriculacion.Asignatura = asignatura;
                return matriculacion;
            },
            param:   new { AlumnoId = alumnoId },
            splitOn: "Id");
    }

    // ─────────────────────────────────────
    // MATRICULACIONES — INSERT
    // ─────────────────────────────────────

    /// <summary>
    /// Matricula a un alumno en una asignatura.
    /// Devuelve el Id generado o lanza excepción si ya existe la matrícula
    /// (la tabla tiene UNIQUE sobre AlumnoId + AsignaturaId).
    /// </summary>
    public async Task<int> MatricularAlumnoAsync(int alumnoId, int asignaturaId)
    {
        // Verificar que el alumno no esté ya matriculado en esa asignatura
        string sqlCheck = @"
            SELECT COUNT(*)
            FROM Matriculaciones
            WHERE AlumnoId = @AlumnoId AND AsignaturaId = @AsignaturaId";

        int existe = await _connection.ExecuteScalarAsync<int>(
            sqlCheck, new { AlumnoId = alumnoId, AsignaturaId = asignaturaId });

        if (existe > 0)
            throw new InvalidOperationException(
                $"El alumno {alumnoId} ya está matriculado en la asignatura {asignaturaId}.");

        string sqlInsert = @"
            INSERT INTO Matriculaciones (AlumnoId, AsignaturaId, FechaAlta, Estado)
            VALUES (@AlumnoId, @AsignaturaId, @FechaAlta, 'Activa');
            SELECT last_insert_rowid();";

        return await _connection.ExecuteScalarAsync<int>(sqlInsert, new
        {
            AlumnoId     = alumnoId,
            AsignaturaId = asignaturaId,
            FechaAlta    = DateTime.Now.ToString("yyyy-MM-dd"),
        });
    }

    // ─────────────────────────────────────
    // INSERT
    // ─────────────────────────────────────

    public async Task<int> InsertarAsync(Asignatura asignatura)
    {
        string sql = @"INSERT INTO Asignaturas (Nombre, Descripcion, Curso, FechaAlta)
                       VALUES (@Nombre, @Descripcion, @Curso, @FechaAlta);
                       SELECT last_insert_rowid();";

        asignatura.FechaAlta = DateTime.Now;
        return await _connection.ExecuteScalarAsync<int>(sql, asignatura);
    }

    // ─────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────

    public async Task<bool> ActualizarAsync(Asignatura asignatura)
    {
        string sql = @"UPDATE Asignaturas
                       SET Nombre      = @Nombre,
                           Descripcion = @Descripcion,
                           Curso   = @Curso
                       WHERE Id = @Id";

        int filasAfectadas = await _connection.ExecuteAsync(sql, asignatura);
        return filasAfectadas > 0;
    }

    // ─────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────

    public async Task<bool> EliminarAsync(int id)
    {
        string sql = "DELETE FROM Asignaturas WHERE Id = @Id";
        int filasAfectadas = await _connection.ExecuteAsync(sql, new { Id = id });
        return filasAfectadas > 0;
    }
}
