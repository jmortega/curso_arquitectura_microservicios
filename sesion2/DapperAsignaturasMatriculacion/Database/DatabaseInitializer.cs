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
        // ── Crear tabla Asignaturas ────────────────────────────────────────
        string crearTablaAsignaturas = @"
            CREATE TABLE IF NOT EXISTS Asignaturas (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre      TEXT    NOT NULL,
                Descripcion TEXT    NOT NULL DEFAULT '',
                Curso       TEXT    NOT NULL DEFAULT 'General',
                FechaAlta   TEXT    NOT NULL
            );";

        await _connection.ExecuteAsync(crearTablaAsignaturas);

        // ── Crear tabla Alumnos ────────────────────────────────────────────
        string crearTablaAlumnos = @"
            CREATE TABLE IF NOT EXISTS Alumnos (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre      TEXT    NOT NULL,
                Apellidos   TEXT    NOT NULL,
                Email       TEXT    NOT NULL UNIQUE,
                Curso       TEXT    NOT NULL DEFAULT 'General',
                FechaNac    TEXT    NOT NULL,
                FechaAlta   TEXT    NOT NULL
            );";

        await _connection.ExecuteAsync(crearTablaAlumnos);

        // ── Crear tabla Matriculaciones ────────────────────────────────────
        // Tabla pivote que relaciona Alumnos ↔ Asignaturas (N:M)
        string crearTablaMatriculaciones = @"
            CREATE TABLE IF NOT EXISTS Matriculaciones (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                AlumnoId     INTEGER NOT NULL,
                AsignaturaId INTEGER NOT NULL,
                FechaAlta    TEXT    NOT NULL,
                Estado       TEXT    NOT NULL DEFAULT 'Activa',  -- Activa | Anulada | Superada
                Nota         REAL,                               -- NULL hasta que haya calificación
                FOREIGN KEY (AlumnoId)     REFERENCES Alumnos    (Id) ON DELETE CASCADE,
                FOREIGN KEY (AsignaturaId) REFERENCES Asignaturas(Id) ON DELETE CASCADE,
                UNIQUE (AlumnoId, AsignaturaId)                  -- un alumno no puede matricularse dos veces
            );";

        await _connection.ExecuteAsync(crearTablaMatriculaciones);

        // ── Datos de ejemplo: Asignaturas ──────────────────────────────────
        int totalAsignaturas = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Asignaturas;");

        if (totalAsignaturas == 0)
        {
            string insertarAsignaturas = @"
                INSERT INTO Asignaturas (Nombre, Descripcion, Curso, FechaAlta) VALUES
                ('Matemáticas',        'Álgebra, cálculo y estadística aplicada',        '1º Bachillerato', '2024-01-15'),
                ('Lengua Castellana',  'Gramática, comprensión lectora y redacción',      '1º Bachillerato', '2024-01-15'),
                ('Física',             'Mecánica, termodinámica y electromagnetismo',     '2º Bachillerato', '2024-01-20'),
                ('Química',            'Estructura atómica, reacciones y estequiometría', '2º Bachillerato', '2024-01-20'),
                ('Historia de España', 'Evolución histórica desde la Edad Media',         '2º Bachillerato', '2024-02-01'),
                ('Inglés',             'Comprensión oral, escrita y conversación',        'General',         '2024-02-01'),
                ('Biología',           'Genética, ecología y fisiología humana',          '1º Bachillerato', '2024-02-10'),
                ('Filosofía',          'Lógica, ética y grandes corrientes filosóficas',  'General',         '2024-02-15');";

            await _connection.ExecuteAsync(insertarAsignaturas);
        }

        // ── Datos de ejemplo: Alumnos ──────────────────────────────────────
        int totalAlumnos = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Alumnos;");

        if (totalAlumnos == 0)
        {
            string insertarAlumnos = @"
                INSERT INTO Alumnos (Nombre, Apellidos, Email, Curso, FechaNac, FechaAlta) VALUES
                ('Carlos',   'García López',    'carlos.garcia@centro.es',   '1º Bachillerato', '2006-03-12', '2024-09-01'),
                ('Laura',    'Martínez Ruiz',   'laura.martinez@centro.es',  '1º Bachillerato', '2006-07-24', '2024-09-01'),
                ('Sergio',   'Fernández Díaz',  'sergio.fdez@centro.es',     '2º Bachillerato', '2005-11-05', '2024-09-01'),
                ('Marta',    'Sánchez Torres',  'marta.sanchez@centro.es',   '2º Bachillerato', '2005-02-18', '2024-09-01'),
                ('Pablo',    'López Moreno',    'pablo.lopez@centro.es',     'General',         '2006-09-30', '2024-09-01'),
                ('Ana',      'Jiménez Vega',    'ana.jimenez@centro.es',     'General',         '2007-01-14', '2024-09-01'),
                ('Diego',    'Romero Castro',   'diego.romero@centro.es',    '1º Bachillerato', '2006-05-22', '2024-09-01'),
                ('Lucía',    'Navarro Gil',     'lucia.navarro@centro.es',   '2º Bachillerato', '2005-08-09', '2024-09-01');";

            await _connection.ExecuteAsync(insertarAlumnos);
        }

        // ── Datos de ejemplo: Matriculaciones ─────────────────────────────
        int totalMatriculaciones = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Matriculaciones;");

        if (totalMatriculaciones == 0)
        {
            string insertarMatriculaciones = @"
                INSERT INTO Matriculaciones (AlumnoId, AsignaturaId, FechaAlta, Estado, Nota) VALUES
                -- Carlos (1º Bachillerato) → Matemáticas(1), Lengua(2), Inglés(6), Biología(7)
                (1, 1, '2024-09-15', 'Superada', 7.5),
                (1, 2, '2024-09-15', 'Superada', 8.0),
                (1, 6, '2024-09-15', 'Activa',   NULL),
                (1, 7, '2024-09-15', 'Activa',   NULL),

                -- Laura (1º Bachillerato) → Matemáticas(1), Lengua(2), Biología(7)
                (2, 1, '2024-09-15', 'Activa',   NULL),
                (2, 2, '2024-09-15', 'Superada', 9.0),
                (2, 7, '2024-09-15', 'Activa',   NULL),

                -- Sergio (2º Bachillerato) → Física(3), Química(4), Historia(5)
                (3, 3, '2024-09-15', 'Superada', 6.5),
                (3, 4, '2024-09-15', 'Activa',   NULL),
                (3, 5, '2024-09-15', 'Activa',   NULL),

                -- Marta (2º Bachillerato) → Física(3), Historia(5), Inglés(6)
                (4, 3, '2024-09-15', 'Anulada',  NULL),
                (4, 5, '2024-09-15', 'Superada', 8.5),
                (4, 6, '2024-09-15', 'Activa',   NULL),

                -- Pablo (General) → Inglés(6), Filosofía(8)
                (5, 6, '2024-09-15', 'Activa',   NULL),
                (5, 8, '2024-09-15', 'Superada', 7.0),

                -- Ana (General) → Inglés(6), Filosofía(8), Matemáticas(1)
                (6, 6, '2024-09-15', 'Activa',   NULL),
                (6, 8, '2024-09-15', 'Activa',   NULL),
                (6, 1, '2024-09-15', 'Superada', 6.0),

                -- Diego (1º Bachillerato) → Matemáticas(1), Biología(7), Inglés(6)
                (7, 1, '2024-09-15', 'Activa',   NULL),
                (7, 7, '2024-09-15', 'Superada', 5.5),
                (7, 6, '2024-09-15', 'Activa',   NULL),

                -- Lucía (2º Bachillerato) → Química(4), Historia(5), Filosofía(8)
                (8, 4, '2024-09-15', 'Superada', 9.5),
                (8, 5, '2024-09-15', 'Activa',   NULL),
                (8, 8, '2024-09-15', 'Activa',   NULL);";

            await _connection.ExecuteAsync(insertarMatriculaciones);
        }
    }
}