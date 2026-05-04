# MediatR Demo — Mediator Pattern · MySQL · Docker · Testcontainers

Demo completo del patrón **Mediator** con MediatR, ASP.NET Core 8, MySQL y tests de integración con Testcontainers.

---

## Arquitectura

```
HTTP Request
     │
     ▼
┌────────────────────────────────────────────────────────┐
│  UsersController (delgado — solo dispatching)          │
│  await _mediator.Send(new CreateUserCommand(...))       │
└────────────────┬───────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────┐
│  MediatR Pipeline                                      │
│  LoggingBehavior → ValidationBehavior → Handler        │
└────────────────┬───────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────┐
│  MySQL (Docker / Testcontainers)                       │
└────────────────────────────────────────────────────────┘

mediator.Publish(UserCreatedNotification)
   ├──► LogUserCreatedHandler
   ├──► SendWelcomeEmailHandler
   └──► AuditUserCreatedHandler
```

---

## Estructura del proyecto

```
MediatRDemo/
├── MediatRDemo.sln
├── Dockerfile
├── docker-compose.yml
│
├── src/MediatRDemo.API/
│   ├── Controllers/UsersController.cs
│   ├── Features/Users/
│   │   ├── Queries/       GetAllUsersQuery, GetUserByIdQuery
│   │   ├── Commands/      CreateUser, UpdateUser, ToggleActive, Delete
│   │   └── Events/        UserCreatedNotification (3 handlers)
│   ├── Application/Behaviors/
│   │   ├── LoggingBehavior.cs      ← IPipelineBehavior
│   │   └── ValidationBehavior.cs  ← IPipelineBehavior
│   ├── Domain/
│   │   ├── Entities/User.cs
│   │   └── Exceptions/
│   └── Infrastructure/
│       ├── Data/AppDbContext.cs    ← EF Core + MySQL
│       └── ExceptionHandlerMiddleware.cs
│
└── tests/MediatRDemo.IntegrationTests/
    ├── Fixtures/
    │   ├── MysqlContainerFixture.cs         ← Testcontainers MySQL
    │   └── CustomWebApplicationFactory.cs  ← WebApplicationFactory
    ├── Users/
    │   ├── GetUsersIntegrationTests.cs
    │   └── CreateUserIntegrationTests.cs
    └── Pipeline/
        └── MediatRPipelineIntegrationTests.cs
```

---

## Ejecutar con Docker Compose

```bash
# Levantar API + MySQL
docker-compose up --build

# Swagger en: http://localhost:8080

# Parar
docker-compose down -v
```

---

## Ejecutar tests de integración

Los tests usan **Testcontainers** — necesitan Docker corriendo en la máquina.

```bash
# Compilar el proyecto primero
dotnet build

# Ejecutar los tests (Testcontainers arranca MySQL automáticamente)
dotnet test tests/MediatRDemo.IntegrationTests

# Con output detallado
dotnet test tests/MediatRDemo.IntegrationTests --logger "console;verbosity=detailed"
```

---

## Tipos de mensajes MediatR

| Tipo | Interfaz | Handlers | Uso |
|---|---|---|---|
| **Query** | `IRequest<T>` | 1 | Leer datos |
| **Command** | `IRequest<T>` | 1 | Modificar estado |
| **Notification** | `INotification` | N | Eventos de dominio |
| **Pipeline Behavior** | `IPipelineBehavior<,>` | — | Cross-cutting concerns |

---

## Endpoints

| Método | Ruta | Handler |
|---|---|---|
| `GET` | `/api/v1/users` | GetAllUsersQueryHandler |
| `GET` | `/api/v1/users/{id}` | GetUserByIdQueryHandler |
| `POST` | `/api/v1/users` | CreateUserCommandHandler |
| `PUT` | `/api/v1/users/{id}` | UpdateUserCommandHandler |
| `PATCH` | `/api/v1/users/{id}/activate` | ToggleUserActiveHandler |
| `PATCH` | `/api/v1/users/{id}/deactivate` | ToggleUserActiveHandler |
| `DELETE` | `/api/v1/users/{id}` | DeleteUserCommandHandler |
