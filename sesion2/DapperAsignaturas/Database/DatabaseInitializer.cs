using System.Data;
using Dapper;

namespace DapperAsignaturas.Database;

public class DatabaseInitializer
{
    private readonly IDbConnection _connection;

    public DatabaseInitializer(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task InicializarAsync()
    {
        // Crear tabla
        string crearTabla = @"
            CREATE TABLE IF NOT EXISTS Asignaturas (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre      TEXT    NOT NULL,
                Descripcion TEXT    NOT NULL DEFAULT '',
                Curso   TEXT    NOT NULL DEFAULT 'General',
                FechaAlta   TEXT    NOT NULL
            );";

        await _connection.ExecuteAsync(crearTabla);

        // Insertar datos de ejemplo solo si la tabla está vacía
        int total = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Asignaturas;");

        if (total == 0)
        {
            string insertarDatos = @"
                INSERT INTO Asignaturas (Nombre, Descripcion, Curso, FechaAlta) VALUES
                ('Matemáticas',              'Álgebra, cálculo y estadística aplicada',         '1º Bachillerato', '2024-01-15'),
                ('Lengua Castellana',        'Gramática, comprensión lectora y redacción',       '1º Bachillerato', '2024-01-15'),
                ('Física',                   'Mecánica, termodinámica y electromagnetismo',      '2º Bachillerato', '2024-01-20'),
                ('Química',                  'Estructura atómica, reacciones y estequiometría',  '2º Bachillerato', '2024-01-20'),
                ('Historia de España',       'Evolución histórica desde la Edad Media',          '2º Bachillerato', '2024-02-01'),
                ('Inglés',                   'Comprensión oral, escrita y conversación',         'General',         '2024-02-01'),
                ('Biología',                 'Genética, ecología y fisiología humana',           '1º Bachillerato', '2024-02-10'),
                ('Filosofía',                'Lógica, ética y grandes corrientes filosóficas',   'General',         '2024-02-15');";
            await _connection.ExecuteAsync(insertarDatos);
        }
    }
}