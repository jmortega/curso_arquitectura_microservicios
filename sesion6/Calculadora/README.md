# Calculadora .NET 8 — Proyecto único con Tests

Proyecto de calculadora en un único `.csproj` que incluye:
- **Lógica de negocio** (`Services/`, `Interfaces/`, `Exceptions/`)
- **Aplicación de consola** (`Program.cs`)
- **Tests unitarios e integración** (`Tests/`)

## Estructura de carpetas

```
Calculadora/
├── Calculadora.csproj          ← proyecto único
├── Program.cs                  ← aplicación de consola
│
├── Interfaces/
│   └── ICalculadora.cs         ← contrato de operaciones
│
├── Exceptions/
│   └── CalculadoraExceptions.cs ← excepciones personalizadas
│
├── Services/
│   ├── CalculadoraService.cs   ← implementación de operaciones
│   └── HistorialService.cs     ← registro de operaciones
│
└── Tests/
    ├── CalculadoraServiceTests.cs  ← tests unitarios (~40 tests)
    ├── HistorialServiceTests.cs    ← tests unitarios (~10 tests)
    └── CalculadoraConHistorialTests.cs ← tests de integración (~8 tests)
```

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Comandos

```bash
# Restaurar paquetes
dotnet restore

# Compilar
dotnet build

# Ejecutar la calculadora (consola interactiva)
dotnet run

# Ejecutar TODOS los tests
dotnet test

# Ejecutar tests con detalle de cada test
dotnet test --logger "console;verbosity=detailed"

# Ejecutar solo los tests de un archivo
dotnet test --filter "FullyQualifiedName~CalculadoraServiceTests"
dotnet test --filter "FullyQualifiedName~HistorialServiceTests"
dotnet test --filter "FullyQualifiedName~CalculadoraConHistorialTests"

# Ejecutar tests con coverage
dotnet test --collect:"XPlat Code Coverage"

# Generar reporte HTML de coverage (requiere reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

## Salida esperada de los tests

```
Passed!  - Failed: 0, Passed: 58, Skipped: 0, Total: 58

  ✓ CalculadoraServiceTests
     Sumar_DosPositivos_RetornaSumaCorrecta
     Sumar_NumeroNegativoYPositivo_RetornaValorCorrecto
     Sumar_DosNegativos_RetornaNegativo
     Sumar_ConCero_RetornaMismoNumero
     Sumar_Decimales_RetornaValorAproximado(a: 2.5, b: 1.5, esperado: 4)
     ...
     Dividir_DivisorCero_LanzaDivisionPorCeroException        ← error esperado
     Dividir_NumeradorYDivisorCero_LanzaDivisionPorCeroException
     RaizCuadrada_NumeroNegativo_LanzaArgumentoInvalidoException
     Porcentaje_PorcentajeNegativo_LanzaArgumentoInvalidoException
     ...

  ✓ HistorialServiceTests
     NuevoHistorial_EstaVacioYTotalEsCero
     Registrar_OperacionValida_SeAgregaAlHistorial
     ...

  ✓ CalculadoraConHistorialTests
     OperacionExitosa_SeRegistraEnHistorial
     MezclaDeExitosYErrores_HistorialContieneAmbos
     ...
```

## Operaciones disponibles

| Operación      | Método                        | Excepciones posibles          |
|----------------|-------------------------------|-------------------------------|
| Suma           | `Sumar(a, b)`                 | `DesbordamientoException`     |
| Resta          | `Restar(a, b)`                | `DesbordamientoException`     |
| Multiplicación | `Multiplicar(a, b)`           | `DesbordamientoException`     |
| División       | `Dividir(a, b)`               | `DivisionPorCeroException`    |
| Potencia       | `Potencia(base, exp)`         | `ArgumentoInvalidoException`  |
| Raíz cuadrada  | `RaizCuadrada(n)`             | `ArgumentoInvalidoException`  |
| Módulo         | `Modulo(a, b)`                | `DivisionPorCeroException`    |
| Porcentaje     | `Porcentaje(valor, pct)`      | `ArgumentoInvalidoException`  |
