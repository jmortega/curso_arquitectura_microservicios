# MediatR Demo — Patrón Mediator en .NET 8

Demo completo del patrón **Mediator** usando la librería **MediatR** con ASP.NET Core, SQLite y Swagger.

---

## Arquitectura

```
HTTP Request
     │
     ▼
┌────────────────────────────────────────┐
│  UsersController (delgado)             │
│  Solo construye Commands/Queries       │
│  y los envía al mediador               │
└────────────────┬───────────────────────┘
                 │  mediator.Send() / mediator.Publish()
                 ▼
┌────────────────────────────────────────┐
│  MediatR Pipeline                      │
│                                        │
│  LoggingBehavior                       │  ← Logging y tiempo de ejecución
│      ↓                                 │
│  ValidationBehavior                    │  ← FluentValidation automático
│      ↓                                 │
│  Handler                               │  ← Lógica de negocio real
└────────────────────────────────────────┘
```

---

## Estructura del proyecto

```
src/MediatRDemo.API/
│
├── Controllers/
│   └── UsersController.cs          ← Solo dispatching, sin lógica
│
├── Features/Users/
│   ├── Queries/
│   │   ├── GetAllUsersQuery.cs     ← Query + Handler
│   │   └── GetUserByIdQuery.cs     ← Query + Handler
│   ├── Commands/
│   │   ├── CreateUserCommand.cs    ← Command + Validator + Handler
│   │   ├── UpdateUserCommand.cs    ← Command + Validator + Handler
│   │   └── ToggleAndDeleteCommands.cs
│   └── Events/
│       └── UserCreatedNotification.cs  ← Notification + 3 Handlers
│
├── Application/Behaviors/
│   ├── LoggingBehavior.cs          ← IPipelineBehavior (logging)
│   └── ValidationBehavior.cs       ← IPipelineBehavior (validación)
│
├── Domain/
│   ├── Entities/User.cs            ← Entidad de dominio
│   └── Exceptions/                 ← Excepciones de dominio
│
└── Infrastructure/
    ├── Data/AppDbContext.cs         ← EF Core + SQLite
    └── ExceptionHandlerMiddleware.cs
```

---

## Tipos de mensajes MediatR

| Tipo | Interfaz | Handlers | Uso |
|---|---|---|---|
| **Query** | `IRequest<T>` | 1 | Leer datos sin modificar estado |
| **Command** | `IRequest<T>` | 1 | Modificar estado del sistema |
| **Notification** | `INotification` | N | Eventos de dominio (múltiples handlers) |
| **Pipeline Behavior** | `IPipelineBehavior<,>` | — | Cross-cutting concerns (logging, validación) |

---

## Ejecución

```bash
dotnet run --project src/MediatRDemo.API

# Swagger en: http://localhost:5000
```

---

## Endpoints

| Método | Ruta | Handler | Descripción |
|---|---|---|---|
| `GET` | `/api/v1/users` | GetAllUsersQueryHandler | Listar usuarios (filtros opcionales) |
| `GET` | `/api/v1/users/{id}` | GetUserByIdQueryHandler | Obtener usuario por ID |
| `POST` | `/api/v1/users` | CreateUserCommandHandler | Crear usuario + publicar evento |
| `PUT` | `/api/v1/users/{id}` | UpdateUserCommandHandler | Actualizar usuario |
| `PATCH` | `/api/v1/users/{id}/activate` | ToggleUserActiveHandler | Activar usuario |
| `PATCH` | `/api/v1/users/{id}/deactivate` | ToggleUserActiveHandler | Desactivar usuario |
| `DELETE` | `/api/v1/users/{id}` | DeleteUserCommandHandler | Eliminar usuario |

---

## Datos semilla

| ID | Nombre | Email | Rol | Activo |
|---|---|---|---|---|
| 11111111-... | Admin User | admin@demo.com | Admin | ✅ |
| 22222222-... | Regular User | user@demo.com | User | ✅ |
| 33333333-... | Read Only | readonly@demo.com | ReadOnly | ❌ |
