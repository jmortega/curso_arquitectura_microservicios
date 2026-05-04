# Enrollments Service — Arquitectura Hexagonal · Microservicios

Microservicio de gestión de **asignaturas y matrículas** construido con ASP.NET Core 8 y arquitectura hexagonal. Se comunica de forma asíncrona con el servicio de estudiantes mediante **RabbitMQ**.

---

## Arquitectura Hexagonal

```
┌──────────────────────────────────────────────────────────────────┐
│  ADAPTADORES DE ENTRADA (API)                                    │
│  SubjectsController  ──►  IMediator  ──►  Command/QueryHandler   │
│  EnrollmentsController                                           │
└───────────────────────────┬──────────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────────┐
│  APLICACIÓN (Casos de uso — MediatR)                             │
│  Commands: CreateSubject, EnrollStudent, CancelEnrollment...     │
│  Queries:  GetAllSubjects, GetEnrollmentsByStudent...            │
│  Behaviors: LoggingBehavior (Decorator), ValidationBehavior      │
└────────────┬──────────────────────────────────────┬─────────────┘
             │ ISubjectRepository                   │ IEventPublisher
             │ IEnrollmentRepository                │ IStudentServiceClient
┌────────────▼──────────────┐         ┌─────────────▼─────────────┐
│  DOMINIO                  │         │  ADAPTADORES DE SALIDA     │
│  Entities, Events,        │         │  SubjectRepository (Dapper)│
│  Factories, Strategies,   │         │  EnrollmentRepository      │
│  Exceptions               │         │  RabbitMqEventPublisher    │
└───────────────────────────┘         │  StudentServiceHttpClient  │
                                      └───────────────────────────┘
```

---

## Patrones de diseño

| Patrón | Implementación | Fichero |
|---|---|---|
| **Repository** | `ISubjectRepository` / `IEnrollmentRepository` | `Infrastructure/Persistence/Repositories/` |
| **Strategy** | `IEnrollmentValidationStrategy` + 3 implementaciones | `Domain/Strategies/ValidationStrategies.cs` |
| **Factory** | `SubjectFactory` / `EnrollmentFactory` | `Domain/Factories/DomainFactories.cs` |
| **Observer** | `IEventPublisher` → RabbitMQ + `StudentDeactivatedEventHandler` | `Infrastructure/Messaging/` + `Application/EventHandlers/` |
| **Decorator** | `LoggingBehavior<TRequest,TResponse>` (IPipelineBehavior) | `Application/Behaviors/Behaviors.cs` |
| **Mediator** | MediatR — desacopla Controllers de Handlers | `Application/Commands/` + `Application/Queries/` |

---

## Comunicación asíncrona con RabbitMQ

```
┌─────────────────────┐   students-events exchange   ┌──────────────────────────┐
│   Students Service  │ ──── student.deactivated ───► │  Enrollments Service     │
│                     │ ──── student.created     ───► │  RabbitMqConsumerService │
│  publishes events   │                               │  → cancela matrículas    │
└─────────────────────┘                               └──────────────────────────┘

┌──────────────────────────┐  enrollments-events exchange  ┌──────────────┐
│   Enrollments Service    │ ─── enrollment.created ──────► │  Consumers   │
│   RabbitMqEventPublisher │ ─── enrollment.cancelled ─────► │  (extensible)│
└──────────────────────────┘                               └──────────────┘
```

**Flujo cuando un alumno es desactivado:**
1. Students-Service publica `student.deactivated` → exchange `students-events`
2. RabbitMQ enruta a la cola `enrollments.student-deactivated`
3. `RabbitMqConsumerService` consume el mensaje
4. `StudentDeactivatedEventHandler` cancela todas las matrículas activas del alumno
5. Para cada matrícula cancelada, se libera la plaza y se publica `enrollment.cancelled`

---

## Ejecutar con Docker Compose

Coloca ambos servicios en la misma carpeta:
```
academic-platform/
├── docker-compose.yml          ← el de este servicio
├── students-service-auth/
└── enrollments-service/
```

```bash
docker-compose up --build

# Endpoints:
#   Students API:    http://localhost:5001  (Swagger)
#   Enrollments API: http://localhost:5002  (Swagger)
#   RabbitMQ UI:     http://localhost:15672 (admin / admin_pass)
```

---

## Endpoints

### Asignaturas (`/api/v1/subjects`)

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/subjects` | Listar asignaturas (`?onlyActive=true`) |
| `GET` | `/api/v1/subjects/{id}` | Obtener por ID |
| `POST` | `/api/v1/subjects` | Crear asignatura |
| `PUT` | `/api/v1/subjects/{id}` | Actualizar asignatura |
| `DELETE` | `/api/v1/subjects/{id}` | Eliminar asignatura |

### Matrículas (`/api/v1/enrollments`)

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/enrollments` | Listar todas |
| `GET` | `/api/v1/enrollments/student/{studentId}` | Matrículas de un alumno |
| `GET` | `/api/v1/enrollments/subject/{subjectId}` | Matrículas de una asignatura |
| `POST` | `/api/v1/enrollments` | Matricular alumno |
| `DELETE` | `/api/v1/enrollments/{id}` | Cancelar matrícula |
