# CryptoConverter API — .NET 8.0

API REST para convertir euros a criptomonedas en tiempo real,
con Swagger UI, tests unitarios, de integración y funcionales.

## Estructura de la solución

```
CryptoConverter/
├── CryptoConverter.sln
│
├── src/
│   └── CryptoConverter.API/               ← API principal
│       ├── Controllers/
│       │   └── ConversionController.cs    ← endpoints REST
│       ├── Services/
│       │   ├── ICryptoService.cs          ← contrato
│       │   └── CryptoService.cs           ← llama a CoinGecko
│       ├── Models/
│       │   └── Models.cs                  ← DTOs y enums
│       ├── Exceptions/
│       │   └── CryptoExceptions.cs        ← excepciones propias
│       ├── Middleware/
│       │   └── ExceptionMiddleware.cs     ← manejo global de errores
│       └── Program.cs                     ← configuración y DI
│
└── tests/
    └── CryptoConverter.Tests/
        ├── Unit/
        │   └── CryptoServiceTests.cs       ← lógica de negocio con MockHttp
        ├── Integration/
        │   └── ConversionControllerIntegrationTests.cs  ← HTTP + Mock de servicio
        ├── Functional/
        │   └── ConversionApiFunctionalTests.cs  ← pipeline completo con CoinGecko simulado
        └── Helpers/
            └── TestHelpers.cs              ← factories y JSON de CoinGecko
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/conversion/precios` | Precios de todas las criptomonedas |
| `GET` | `/api/conversion/precios/{moneda}` | Precio de una moneda concreta |
| `POST` | `/api/conversion/convertir` | Conversión con body JSON |
| `GET` | `/api/conversion/convertir?euros=1000&moneda=BTC` | Conversión rápida por query |

**Monedas soportadas:** `BTC`, `ETH`, `BNB`, `SOL`, `ADA`

## Ejecutar la API

```bash
cd src/CryptoConverter.API
dotnet run

# Swagger UI disponible en:
# http://localhost:5000
```

## Ejecutar los tests

```bash
# Todos los tests
dotnet test

# Solo tests unitarios
dotnet test --filter "FullyQualifiedName~Unit"

# Solo tests de integración
dotnet test --filter "FullyQualifiedName~Integration"

# Solo tests funcionales
dotnet test --filter "FullyQualifiedName~Functional"

# Con detalle
dotnet test --logger "console;verbosity=detailed"

# Con coverage
dotnet test tests/CryptoConverter.Tests \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=./coverage/coverage.cobertura.xml

# Reporte HTML de coverage
reportgenerator \
  -reports:"tests/CryptoConverter.Tests/coverage/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
```

## Diferencias entre tipos de test

| Tipo | Qué prueba | Dependencias externas |
|---|---|---|
| **Unitario** | Lógica de CryptoService aislada | HttpClient simulado con MockHttp |
| **Integración** | Controlador + pipeline HTTP | ICryptoService mockeado con Moq |
| **Funcional** | App completa extremo a extremo | CoinGecko simulado con MockHttp |

## Ejemplos de uso

```bash
# Precio de Bitcoin
curl http://localhost:5000/api/conversion/precios/BTC

# Todos los precios
curl http://localhost:5000/api/conversion/precios

# Conversión rápida
curl "http://localhost:5000/api/conversion/convertir?euros=1000&moneda=BTC"

# Conversión con body JSON
curl -X POST http://localhost:5000/api/conversion/convertir \
  -H "Content-Type: application/json" \
  -d '{"euros": 1000, "moneda": "ETH"}'
```
