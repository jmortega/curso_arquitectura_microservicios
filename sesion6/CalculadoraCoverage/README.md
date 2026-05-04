# Calculadora .NET 8

## Estructura

```
Calculadora/
├── Calculadora.sln
│
├── src/
│   ├── Calculadora.Core/          ← librería (lógica de negocio)
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   └── Services/
│   │
│   └── Calculadora.App/           ← consola (OutputType=Exe)
│       └── Program.cs
│
└── tests/
    └── Calculadora.Tests/         ← proyecto de tests (sin OutputType=Exe)
        ├── CalculadoraServiceTests.cs
        ├── HistorialServiceTests.cs
        └── CalculadoraConHistorialTests.cs
```

## Por qué dos proyectos

Coverlet **no puede generar coverage de un ensamblado `OutputType=Exe`**.
La solución es separar la lógica en `Calculadora.Core` (tipo librería) y
apuntar los tests a ese proyecto. El ejecutable `Calculadora.App` queda
fuera del coverage porque no contiene lógica de negocio.

## Comandos

```bash
# Restaurar paquetes
dotnet restore

# Ejecutar la calculadora
dotnet run --project src/Calculadora.App

# Ejecutar todos los tests
dotnet test

# ── Coverage ──────────────────────────────────────────────────────────

# Opción A: coverlet.msbuild (recomendado, ruta fija)
dotnet test tests/Calculadora.Tests \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=./coverage/coverage.cobertura.xml

# Opción B: XPlat collector (genera GUID en la ruta)
dotnet test tests/Calculadora.Tests \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# ── Reporte HTML ───────────────────────────────────────────────────────

# Instalar reportgenerator (solo la primera vez)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generar reporte (usar la ruta según la opción elegida arriba)
reportgenerator \
  -reports:"tests/Calculadora.Tests/coverage/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Abrir el reporte
xdg-open coverage-report/index.html   # Linux
start coverage-report/index.html      # Windows
open  coverage-report/index.html      # macOS
```

## Resultado esperado

```
Passed! - Failed: 0, Passed: 58, Skipped: 0, Total: 58

+------------------+--------+--------+--------+
| Module           | Line   | Branch | Method |
+------------------+--------+--------+--------+
| Calculadora.Core | 95%    | 91%    | 100%   |
+------------------+--------+--------+--------+
```
