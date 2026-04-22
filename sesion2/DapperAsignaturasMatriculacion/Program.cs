using System.Data;
using Microsoft.Data.Sqlite;
using DapperAsignaturas.Database;
using DapperAsignaturas;
using DapperAsignaturas.Repositories;

// ─────────────────────────────────────────────────
// CONFIGURACIÓN E INICIALIZACIÓN
// ─────────────────────────────────────────────────
const string connectionString = "Data Source=asignaturas.db";
IDbConnection connection = new SqliteConnection(connectionString);

var dbInit = new DatabaseInitializer(connection);
await dbInit.InicializarAsync();

IAsignaturaRepository repo = new AsignaturaRepository(connection);

// ─────────────────────────────────────────────────
// BUCLE PRINCIPAL DEL MENÚ
// ─────────────────────────────────────────────────
bool salir = false;

while (!salir)
{
    MostrarMenuPrincipal();
    string opcion = Console.ReadLine()?.Trim() ?? "";

    switch (opcion)
    {
        case "1":  await MostrarTodasAsignaturasAsync();        break;
        case "2":  await MostrarTodasAsignaturasAsyncDynamic(); break;
        case "3":  await BuscarAsignaturaPorIdAsync();          break;
        case "4":  await BuscarAsignaturaPorNombreAsync();      break;
        case "5":  await FiltrarPorCursoAsync();                break;
        case "6":  await MostrarEstadisticasAsync();            break;
        case "7":  await ExportarCSVAsignaturaAsync();          break;
        case "8":  await InsertarAsignaturaAsync();             break;
        case "9":  await ActualizarAsignaturaAsync();           break;
        case "10": await EliminarAsignaturaAsync();             break;
        case "11": await MostrarMatriculacionesAlumnoAsync();   break;
        case "12": await MatricularAlumnoAsync();               break;
        case "0":  salir = true; MostrarMensaje("👋 ¡Hasta pronto!", ConsoleColor.Cyan); break;
        default:   MostrarError("Opción no válida. Introduce un número del 0 al 12.");   break;
    }

    if (!salir)
    {
        Console.WriteLine("\nPulsa ENTER para volver al menú...");
        Console.ReadLine();
    }
}

// ─────────────────────────────────────────────────
// MENÚ
// ─────────────────────────────────────────────────
void MostrarMenuPrincipal()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("""
        ╔══════════════════════════════════════════╗
        ║        GESTIÓN DE ASIGNATURAS            ║
        ║           Dapper + SQLite                ║
        ╠══════════════════════════════════════════╣
        ║  CONSULTAS                               ║
        ║  [1]  Ver todas las asignaturas          ║
        ║  [2]  Ver todas las asignaturas Dynamic  ║
        ║  [3]  Buscar asignatura por ID           ║
        ║  [4]  Buscar asignatura por nombre       ║
        ║  [5]  Filtrar por curso                  ║
        ║  [6]  Ver estadísticas                   ║
        ║  [7]  Exportar CSV asignaturas           ║
        ╠══════════════════════════════════════════╣
        ║  GESTIÓN                                 ║
        ║  [8]  Insertar nueva asignatura          ║
        ║  [9]  Actualizar asignatura              ║
        ║  [10] Eliminar asignatura                ║
        ╠══════════════════════════════════════════╣
        ║  MATRICULACIONES                         ║
        ║  [11] Ver matriculaciones de un alumno   ║
        ║  [12] Matricular alumno en asignatura    ║
        ╠══════════════════════════════════════════╣
        ║  [0]  Salir                              ║
        ╚══════════════════════════════════════════╝
        """);
    Console.ResetColor();
    Console.Write("  Selecciona una opción: ");
}

// ─────────────────────────────────────────────────
// OPCIÓN 1 — VER TODOS LAS ASIGNATURAS
// ─────────────────────────────────────────────────
async Task MostrarTodasAsignaturasAsync()
{
    MostrarTitulo("LISTADO COMPLETO DE ASIGNATURAS");

    var asignaturas = await repo.ObtenerTodosAsync();
    var lista = asignaturas.ToList();

    if (!lista.Any())
    {
        MostrarError("No hay asignaturas registradas.");
        return;
    }

    MostrarTablaAsignaturas(lista);
    MostrarMensaje($"\n  Total: {lista.Count} asignatura(s) encontrada(s).", ConsoleColor.Yellow);
}

async Task MostrarTodasAsignaturasAsyncDynamic()
{
    MostrarTitulo("LISTADO COMPLETO DE ASIGNATURAS (DYNAMIC)");

    var asignaturas = await repo.ObtenerTodosAsyncDynamic();
    MostrarTablaAsignaturasDynamic(asignaturas.ToList());
}

async Task ExportarCSVAsignaturaAsync()
{
    MostrarTitulo("EXPORTAR CSV DE ASIGNATURAS");

    var asignaturas = await repo.ObtenerTodos();

    await using var writer = new StreamWriter("asignaturas.csv");
    foreach (var a in asignaturas)
        await writer.WriteLineAsync($"{a.Id},{a.Nombre},{a.Descripcion},{a.Curso},{a.FechaAlta}");

    MostrarMensaje("  ✅ Fichero asignaturas.csv generado correctamente.", ConsoleColor.Green);
}

// ─────────────────────────────────────────────────
// OPCIÓN 2 — BUSCAR POR ID
// ─────────────────────────────────────────────────
async Task BuscarAsignaturaPorIdAsync()
{
    MostrarTitulo("BUSCAR ASIGNATURA POR ID");

    int id = LeerEntero("  Introduce el ID de la asignatura: ");
    var asignatura = await repo.ObtenerPorIdAsync(id);

    if (asignatura is null)
    {
        MostrarError($"No se encontró ninguna asignatura con ID {id}.");
        return;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(asignatura);
    Console.ResetColor();
}

// ─────────────────────────────────────────────────
// OPCIÓN 3 — BUSCAR POR NOMBRE
// ─────────────────────────────────────────────────
async Task BuscarAsignaturaPorNombreAsync()
{
    MostrarTitulo("BUSCAR ASIGNATURA POR NOMBRE");

    Console.Write("  Introduce el nombre (o parte del nombre): ");
    string nombre = Console.ReadLine()?.Trim() ?? "";

    if (string.IsNullOrEmpty(nombre))
    {
        MostrarError("El nombre no puede estar vacío.");
        return;
    }

    var asignaturas = await repo.BuscarPorNombreAsync(nombre);
    var lista = asignaturas.ToList();

    if (!lista.Any())
    {
        MostrarError($"No se encontraron asignaturas que contengan '{nombre}'.");
        return;
    }

    MostrarTablaAsignaturas(lista);
    MostrarMensaje($"\n  {lista.Count} resultado(s) para '{nombre}'.", ConsoleColor.Yellow);
}

// ─────────────────────────────────────────────────
// OPCIÓN 4 — FILTRAR POR CURSO
// ─────────────────────────────────────────────────
async Task FiltrarPorCursoAsync()
{
    MostrarTitulo("FILTRAR POR CURSO");

    Console.WriteLine("  Cursos disponibles: 1º Bachillerato, 2º Bachillerato, General");
    Console.Write("  Introduce el curso: ");
    string curso = Console.ReadLine()?.Trim() ?? "";

    if (string.IsNullOrEmpty(curso))
    {
        MostrarError("El curso no puede estar vacío.");
        return;
    }

    var asignaturas = await repo.ObtenerPorCursoAsync(curso);
    var lista = asignaturas.ToList();

    if (!lista.Any())
    {
        MostrarError($"No hay asignaturas en el curso '{curso}'.");
        return;
    }

    MostrarTablaAsignaturas(lista);
    MostrarMensaje($"\n  {lista.Count} asignatura(s) en '{curso}'.", ConsoleColor.Yellow);
}

// ─────────────────────────────────────────────────
// OPCIÓN 6 — ESTADÍSTICAS
// ─────────────────────────────────────────────────
async Task MostrarEstadisticasAsync()
{
    MostrarTitulo("ESTADÍSTICAS");

    int total = await repo.ObtenerTotalAsignaturasAsync();

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"""

          📦 Total de Asignaturas: {total}
        """);
    Console.ResetColor();
}

// ─────────────────────────────────────────────────
// OPCIÓN 8 — INSERTAR ASIGNATURA
// ─────────────────────────────────────────────────
async Task InsertarAsignaturaAsync()
{
    MostrarTitulo("INSERTAR NUEVA ASIGNATURA");

    var asignatura = new Asignatura
    {
        Nombre      = LeerTextoObligatorio("  Nombre:      "),
        Descripcion = LeerTextoObligatorio("  Descripción: "),
        Curso       = LeerTextoObligatorio("  Curso:       "),
    };

    Console.WriteLine();
    MostrarMensaje("  Resumen de la nueva asignatura:", ConsoleColor.Cyan);
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"""
          Nombre:      {asignatura.Nombre}
          Descripción: {asignatura.Descripcion}
          Curso:       {asignatura.Curso}
        """);
    Console.ResetColor();

    if (!Confirmar("  ¿Confirmas la inserción? (s/n): ")) return;

    int nuevoId = await repo.InsertarAsync(asignatura);
    MostrarMensaje($"  ✅ Asignatura insertada correctamente con ID {nuevoId}.", ConsoleColor.Green);
}

// ─────────────────────────────────────────────────
// OPCIÓN 9 — ACTUALIZAR ASIGNATURA
// ─────────────────────────────────────────────────
async Task ActualizarAsignaturaAsync()
{
    MostrarTitulo("ACTUALIZAR ASIGNATURA");

    int id = LeerEntero("  ID de la asignatura a actualizar: ");
    var existente = await repo.ObtenerPorIdAsync(id);

    if (existente is null)
    {
        MostrarError($"No existe ninguna asignatura con ID {id}.");
        return;
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n  Asignatura actual:");
    Console.WriteLine(existente);
    Console.ResetColor();

    Console.WriteLine("  Introduce los nuevos valores (ENTER para mantener el actual):\n");

    existente.Nombre      = LeerTextoOpcional($"  Nombre [{existente.Nombre}]: ",           existente.Nombre);
    existente.Descripcion = LeerTextoOpcional($"  Descripción [{existente.Descripcion}]: ",  existente.Descripcion);
    existente.Curso       = LeerTextoOpcional($"  Curso [{existente.Curso}]: ",              existente.Curso);

    if (!Confirmar("\n  ¿Confirmas los cambios? (s/n): ")) return;

    bool ok = await repo.ActualizarAsync(existente);

    if (ok) MostrarMensaje("  ✅ Asignatura actualizada correctamente.", ConsoleColor.Green);
    else    MostrarError("  ❌ No se pudo actualizar la asignatura.");
}

// ─────────────────────────────────────────────────
// OPCIÓN 10 — ELIMINAR ASIGNATURA
// ─────────────────────────────────────────────────
async Task EliminarAsignaturaAsync()
{
    MostrarTitulo("ELIMINAR ASIGNATURA");

    int id = LeerEntero("  ID de la asignatura a eliminar: ");
    var existente = await repo.ObtenerPorIdAsync(id);

    if (existente is null)
    {
        MostrarError($"No existe ninguna asignatura con ID {id}.");
        return;
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n  Asignatura a eliminar:");
    Console.WriteLine(existente);
    Console.ResetColor();

    MostrarError("  ⚠️  Esta acción no se puede deshacer.");

    if (!Confirmar("  ¿Confirmas la eliminación? (s/n): ")) return;

    bool ok = await repo.EliminarAsync(id);

    if (ok) MostrarMensaje("  ✅ Asignatura eliminada correctamente.", ConsoleColor.Green);
    else    MostrarError("  ❌ No se pudo eliminar la asignatura.");
}

// ─────────────────────────────────────────────────
// OPCIÓN 11 — VER MATRICULACIONES DE UN ALUMNO
// ─────────────────────────────────────────────────
async Task MostrarMatriculacionesAlumnoAsync()
{
    MostrarTitulo("MATRICULACIONES DE UN ALUMNO");

    int alumnoId = LeerEntero("  Introduce el ID del alumno: ");

    var matriculaciones = await repo.ObtenerMatriculacionesPorAlumnoAsync(alumnoId);
    var lista = matriculaciones.ToList();

    if (!lista.Any())
    {
        MostrarError($"El alumno con ID {alumnoId} no tiene matriculaciones registradas.");
        return;
    }

    // Cabecera de la tabla
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"\n  {"ID",-5} {"Asignatura",-25} {"Curso",-20} {"Estado",-10} {"Nota",-6} {"Fecha Alta"}");
    Console.WriteLine($"  {"─────",-5} {"─────────────────────────",-25} {"────────────────────",-20} {"──────────",-10} {"──────",-6} {"──────────"}");

    foreach (var m in lista)
    {
        // Colorear según el estado de la matriculación
        Console.ForegroundColor = m.Estado switch
        {
            "Superada" => ConsoleColor.Green,
            "Anulada"  => ConsoleColor.Red,
            _          => ConsoleColor.Gray,     // Activa
        };

        string asignaturaNombre = m.Asignatura.Nombre.Length > 24
            ? m.Asignatura.Nombre[..21] + "..."
            : m.Asignatura.Nombre;

        string nota = m.Nota.HasValue ? m.Nota.Value.ToString("F1") : "─";

        Console.WriteLine($"  {m.Id,-5} {asignaturaNombre,-25} {m.Asignatura.Curso,-20} {m.Estado,-10} {nota,-6} {m.FechaAlta}");
    }

    Console.ResetColor();
    MostrarMensaje($"\n  Total: {lista.Count} matriculación(es) encontrada(s).", ConsoleColor.Yellow);
}

// ─────────────────────────────────────────────────
// OPCIÓN 12 — MATRICULAR ALUMNO EN ASIGNATURA
// ─────────────────────────────────────────────────
async Task MatricularAlumnoAsync()
{
    MostrarTitulo("MATRICULAR ALUMNO EN ASIGNATURA");

    int alumnoId     = LeerEntero("  ID del alumno:      ");
    int asignaturaId = LeerEntero("  ID de asignatura:   ");

    // Mostrar resumen antes de confirmar
    var asignatura = await repo.ObtenerPorIdAsync(asignaturaId);
    if (asignatura is null)
    {
        MostrarError($"No existe ninguna asignatura con ID {asignaturaId}.");
        return;
    }

    Console.WriteLine();
    MostrarMensaje("  Resumen de la matriculación:", ConsoleColor.Cyan);
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"""
          Alumno ID:   {alumnoId}
          Asignatura:  {asignatura.Nombre}  (ID: {asignatura.Id})
          Curso:       {asignatura.Curso}
          Estado:      Activa
        """);
    Console.ResetColor();

    if (!Confirmar("  ¿Confirmas la matriculación? (s/n): ")) return;

    try
    {
        int nuevaMatriculaId = await repo.MatricularAlumnoAsync(alumnoId, asignaturaId);
        MostrarMensaje(
            $"  ✅ Alumno matriculado correctamente. ID de matrícula: {nuevaMatriculaId}.",
            ConsoleColor.Green);
    }
    catch (InvalidOperationException ex)
    {
        // El alumno ya estaba matriculado en esa asignatura
        MostrarError($"  ❌ {ex.Message}");
    }
}

// ─────────────────────────────────────────────────
// HELPERS — VISUALIZACIÓN
// ─────────────────────────────────────────────────
void MostrarTitulo(string titulo)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n  ── {titulo} ──\n");
    Console.ResetColor();
}

void MostrarMensaje(string mensaje, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.WriteLine(mensaje);
    Console.ResetColor();
}

void MostrarError(string mensaje)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n  {mensaje}");
    Console.ResetColor();
}

void MostrarTablaAsignaturas(List<Asignatura> lista)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  {"ID",-5} {"Nombre",-25} {"Curso",-15}");
    Console.WriteLine($"  {"─────",-5} {"─────────────────────────",-25} {"───────────────",-15}");

    foreach (var p in lista)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        string nombre = p.Nombre.Length > 24 ? p.Nombre[..21] + "..." : p.Nombre;
        Console.WriteLine($"  {p.Id,-5} {nombre,-25} {p.Curso,-15}");
    }

    Console.ResetColor();
}

void MostrarTablaAsignaturasDynamic(List<dynamic> lista)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  {"ID",-5} {"Nombre",-25} {"Curso",-15}");
    Console.WriteLine($"  {"─────",-5} {"─────────────────────────",-25} {"───────────────",-15}");

    foreach (var p in lista)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        string nombre = p.Nombre.Length > 24 ? p.Nombre[..21] + "..." : p.Nombre;
        Console.WriteLine($"  {p.Id,-5} {nombre,-25} {p.Curso,-15}");
    }

    Console.ResetColor();
}

// ─────────────────────────────────────────────────
// HELPERS — ENTRADA DE DATOS
// ─────────────────────────────────────────────────
int LeerEntero(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int valor) && valor >= 0)
            return valor;
        MostrarError("  Valor no válido. Introduce un número entero positivo.");
    }
}

int LeerEnteroOpcional(string prompt, int valorActual)
{
    Console.Write(prompt);
    string input = Console.ReadLine()?.Trim() ?? "";
    return string.IsNullOrEmpty(input) ? valorActual :
           int.TryParse(input, out int v) && v >= 0 ? v : valorActual;
}

decimal LeerDecimal(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal valor) && valor >= 0)
            return valor;
        MostrarError("  Valor no válido. Introduce un número decimal positivo.");
    }
}

decimal LeerDecimalOpcional(string prompt, decimal valorActual)
{
    Console.Write(prompt);
    string input = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrEmpty(input)) return valorActual;
    return decimal.TryParse(input.Replace(',', '.'),
        System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out decimal v) && v >= 0
        ? v : valorActual;
}

string LeerTextoObligatorio(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string valor = Console.ReadLine()?.Trim() ?? "";
        if (!string.IsNullOrEmpty(valor)) return valor;
        MostrarError("  Este campo es obligatorio.");
    }
}

string LeerTextoOpcional(string prompt, string valorActual)
{
    Console.Write(prompt);
    string input = Console.ReadLine()?.Trim() ?? "";
    return string.IsNullOrEmpty(input) ? valorActual : input;
}

bool Confirmar(string prompt)
{
    Console.Write(prompt);
    string respuesta = Console.ReadLine()?.Trim().ToLower() ?? "";
    if (respuesta != "s")
    {
        MostrarMensaje("  Operación cancelada.", ConsoleColor.Yellow);
        return false;
    }
    return true;
}