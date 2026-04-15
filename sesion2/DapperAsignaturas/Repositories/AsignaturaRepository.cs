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
        string sql = @"UPDATE Productos
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