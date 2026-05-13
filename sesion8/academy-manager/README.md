# Academy Manager — CQRS + Arquitectura Hexagonal en .NET 8

Proyecto de demostración de los patrones **CQRS** y **Arquitectura Hexagonal** usando .NET 8, con base de datos separadas para lectura y escritura y soporte de despliegue en Docker Compose y Kubernetes (Minikube).

---

## Tabla de contenidos

1. [Arquitectura](#arquitectura)
2. [Estructura del proyecto](#estructura-del-proyecto)
3. [Flujo CQRS](#flujo-cqrs)
4. [Requisitos previos](#requisitos-previos)
5. [Despliegue con Docker Compose](#despliegue-con-docker-compose)
6. [Despliegue en Kubernetes con Minikube](#despliegue-en-kubernetes-con-minikube)
7. [API Reference](#api-reference)
8. [Migraciones EF Core](#migraciones-ef-core)
9. [Decisiones de diseño](#decisiones-de-diseño)

---

## Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Layer (.NET 8)                        │
│   StudentsController  SubjectsController  EnrollmentsController  │
└──────────────────────────┬──────────────────────────────────────┘
                           │ IMediator
          ┌────────────────┴────────────────┐
          │                                 │
   ┌──────▼──────┐                  ┌───────▼──────┐
   │  COMMANDS   │                  │   QUERIES    │
   │  (Writes)   │                  │   (Reads)    │
   └──────┬──────┘                  └───────┬──────┘
          │ Handler                         │ Handler
          │                                 │
   ┌──────▼──────────┐             ┌────────▼────────┐
   │  Domain Layer   │             │  Read Repos     │
   │  Aggregates     │             │  (MongoDB port) │
   │  Domain Events  │             └────────┬────────┘
   └──────┬──────────┘                      │
          │ IRepository (port)              │
          │                                 │
   ┌──────▼──────────────────────┐  ┌───────▼──────────────┐
   │  Infrastructure Write Side  │  │  Infrastructure Read  │
   │  EF Core + PostgreSQL       │  │  MongoDB Driver       │
   │  (Write DB — normalizado)   │  │  (Read DB — desnorm.) │
   └──────┬──────────────────────┘  └──────────────────────┘
          │ Domain Events (via MediatR Publish)
          │
   ┌──────▼──────────────────────────────────────────────────┐
   │  Event Handlers (Infrastructure)                         │
   │  Proyectan cambios del Write DB → Read DB (MongoDB)     │
   └─────────────────────────────────────────────────────────┘
```

### Bases de datos

| Base de datos | Motor | Puerto | Rol | Características |
|---|---|---|---|---|
| `academy_write` | PostgreSQL 16 | 5432 | Write side | Normalizada, ACID, EF Core |
| `academy_read` | MongoDB 7 | 27017 | Read side | Desnormalizada, optimizada para queries |

---

## Estructura del proyecto

```
academy-manager/
├── src/
│   ├── AcademyManager.Domain/          # Núcleo — sin dependencias externas
│   │   ├── Common/                     # Entity<T>, ValueObject, IDomainEvent, Result
│   │   ├── Students/                   # Agregado Student + value objects + eventos
│   │   ├── Subjects/                   # Agregado Subject + eventos
│   │   └── Enrollments/               # Agregado Enrollment + eventos
│   │
│   ├── AcademyManager.Application/    # Casos de uso — depende solo del dominio
│   │   ├── Common/
│   │   │   ├── Behaviors/             # ValidationBehavior, LoggingBehavior (pipeline MediatR)
│   │   │   └── Interfaces/            # Puertos de lectura (IStudentReadRepository, ...)
│   │   ├── Students/
│   │   │   ├── Commands/              # CreateStudent, UpdateStudent, DeleteStudent
│   │   │   └── Queries/               # GetStudentById, GetAllStudents
│   │   ├── Subjects/
│   │   ├── Enrollments/
│   │   └── ReadModels/                # DTOs para MongoDB (StudentReadModel, ...)
│   │
│   ├── AcademyManager.Infrastructure/ # Adaptadores — implementa los puertos del dominio
│   │   ├── Persistence/
│   │   │   ├── Write/                 # WriteDbContext (EF Core + PostgreSQL)
│   │   │   │   ├── Configurations/    # Fluent API mappings
│   │   │   │   └── Repositories/      # Implementaciones write-side
│   │   │   └── Read/                  # MongoDbContext + read repositories
│   │   ├── EventHandlers/             # Proyectan domain events → MongoDB read models
│   │   └── Migrations/                # EF Core migrations + design-time factory
│   │
│   └── AcademyManager.API/            # Adaptador HTTP
│       ├── Controllers/               # StudentsController, SubjectsController, ...
│       ├── Extensions/                # GlobalExceptionMiddleware
│       └── Program.cs
│
├── k8s/                               # Manifiestos Kubernetes
│   ├── namespace.yaml
│   ├── secrets/
│   ├── configmaps/
│   ├── postgres/
│   ├── mongodb/
│   ├── api/
│   └── ingress/
├── scripts/                           # Init scripts para DBs
├── Dockerfile                         # Multi-stage build
├── docker-compose.yml
└── README.md
```

---

## Flujo CQRS

### Comando (Write path)

```
HTTP POST /api/students
       │
       ▼
StudentsController.Create()
       │  IMediator.Send(CreateStudentCommand)
       ▼
ValidationBehavior  →  valida con FluentValidation
       │
       ▼
LoggingBehavior     →  registra timing
       │
       ▼
CreateStudentHandler
  1. Student.Create(...)       → crea agregado + añade StudentCreatedEvent
  2. repository.AddAsync()     → persiste en PostgreSQL (Write DB)
  3. SaveChangesAsync()        → EF Core guarda + dispara domain events
       │
       ▼
StudentCreatedEventHandler   (INotificationHandler<StudentCreatedEvent>)
  → Crea StudentReadModel y lo upserta en MongoDB (Read DB)
```

### Query (Read path)

```
HTTP GET /api/students/{id}
       │
       ▼
StudentsController.GetById()
       │  IMediator.Send(GetStudentByIdQuery)
       ▼
LoggingBehavior
       │
       ▼
GetStudentByIdHandler
  → IStudentReadRepository.GetByIdAsync()
  → Consulta directamente MongoDB (Read DB)
  → Devuelve StudentReadModel desnormalizado
```

---

## Requisitos previos

### Para Docker Compose

- [Docker Desktop](https://docs.docker.com/get-docker/) ≥ 24 o Docker Engine + Compose plugin
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (solo si quieres ejecutar sin Docker)

### Para Kubernetes / Minikube

- [Minikube](https://minikube.sigs.k8s.io/docs/start/) ≥ 1.32
- [kubectl](https://kubernetes.io/docs/tasks/tools/) ≥ 1.28
- [Docker](https://docs.docker.com/get-docker/)
- Mínimo 4 GB de RAM y 4 CPUs asignados a Minikube

---

## Despliegue con Docker Compose

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-org/academy-manager.git
cd academy-manager
```

### 2. Construir la imagen de la API

```bash
docker compose build academy-api
```

### 3. Levantar los servicios

```bash
# Levanta PostgreSQL (write), MongoDB (read) y la API
docker compose up -d

# Comprobar que todos los contenedores están healthy
docker compose ps
```

Salida esperada:

```
NAME                       STATUS          PORTS
academy-postgres-write     Up (healthy)    0.0.0.0:5432->5432/tcp
academy-mongodb-read       Up (healthy)    0.0.0.0:27017->27017/tcp
academy-api                Up (healthy)    0.0.0.0:5000->8080/tcp
```

### 4. Verificar el despliegue

```bash
# Swagger UI (documentación interactiva)
open http://localhost:5000

# Health check
curl http://localhost:5000/health

# Crear un alumno de prueba
curl -s -X POST http://localhost:5000/api/students \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "María",
    "lastName": "García",
    "email": "maria.garcia@universidad.es",
    "dateOfBirth": "2000-03-15T00:00:00Z"
  }' | jq .

# Listar alumnos (desde MongoDB read DB)
curl -s http://localhost:5000/api/students | jq .
```

### 5. Levantar herramientas opcionales (Mongo Express)

```bash
# Interfaz web para MongoDB en http://localhost:8081
docker compose --profile tools up -d mongo-express
```

### 6. Ver logs

```bash
# Todos los servicios
docker compose logs -f

# Solo la API
docker compose logs -f academy-api
```

### 7. Parar y limpiar

```bash
# Parar sin borrar volúmenes
docker compose down

# Parar Y borrar todos los datos (volúmenes)
docker compose down -v
```

---

## Despliegue en Kubernetes con Minikube

### 1. Iniciar Minikube

```bash
# Iniciar con recursos suficientes
minikube start \
  --driver=docker \
  --cpus=4 \
  --memory=4096 \
  --kubernetes-version=v1.29.0

# Verificar que el clúster está operativo
kubectl cluster-info
minikube status
```

### 2. Habilitar addons necesarios

```bash
# Ingress controller (nginx)
minikube addons enable ingress

# Metrics server (necesario para HPA)
minikube addons enable metrics-server

# Verificar addons
minikube addons list | grep -E "ingress|metrics"
```

### 3. Construir y cargar la imagen en Minikube

Minikube tiene su propio registro Docker interno. Hay dos opciones:

**Opción A — Usar el daemon Docker de Minikube (recomendado):**

```bash
# Apuntar el CLI de Docker al daemon de Minikube
eval $(minikube docker-env)

# Construir la imagen directamente en Minikube
docker build -t academy-manager-api:latest .

# Verificar que la imagen está disponible
docker images | grep academy-manager-api
```

**Opción B — Cargar imagen existente:**

```bash
# Construir localmente
docker build -t academy-manager-api:latest .

# Cargar en Minikube
minikube image load academy-manager-api:latest
```

> **Importante:** El Deployment usa `imagePullPolicy: Never` para que Kubernetes use
> la imagen local sin intentar descargarla de un registry externo.

### 4. Aplicar los manifiestos

```bash
# 1. Namespace
kubectl apply -f k8s/namespace.yaml

# 2. Secrets y ConfigMaps
kubectl apply -f k8s/secrets/db-secrets.yaml
kubectl apply -f k8s/configmaps/app-config.yaml

# 3. Base de datos de escritura (PostgreSQL)
kubectl apply -f k8s/postgres/postgres.yaml

# 4. Base de datos de lectura (MongoDB)
kubectl apply -f k8s/mongodb/mongodb.yaml

# 5. Esperar a que las DBs estén Ready
kubectl wait --for=condition=ready pod \
  -l app=postgres-write \
  -n academy \
  --timeout=120s

kubectl wait --for=condition=ready pod \
  -l app=mongodb-read \
  -n academy \
  --timeout=120s

# 6. Desplegar la API
kubectl apply -f k8s/api/deployment.yaml

# 7. Ingress
kubectl apply -f k8s/ingress/ingress.yaml
```

O bien aplicar todo de una vez:

```bash
kubectl apply -f k8s/ --recursive
```

### 5. Verificar el despliegue

```bash
# Ver todos los recursos del namespace academy
kubectl get all -n academy

# Verificar pods
kubectl get pods -n academy -w

# Verificar servicios
kubectl get svc -n academy

# Verificar ingress
kubectl get ingress -n academy

# Logs de la API (reemplaza <pod-name> con el nombre real)
kubectl logs -n academy -l app=academy-api -f

# Eventos del namespace (útil para depurar)
kubectl get events -n academy --sort-by='.lastTimestamp'
```

Salida esperada de `kubectl get pods -n academy`:

```
NAME                             READY   STATUS    RESTARTS   AGE
postgres-write-xxx               1/1     Running   0          2m
mongodb-read-xxx                 1/1     Running   0          2m
academy-api-xxx-yyy              1/1     Running   0          90s
academy-api-xxx-zzz              1/1     Running   0          90s
```

### 6. Acceder a la aplicación

#### Vía Ingress (recomendado)

```bash
# Obtener la IP de Minikube
MINIKUBE_IP=$(minikube ip)
echo "Minikube IP: $MINIKUBE_IP"

# Añadir entrada en /etc/hosts (requiere sudo)
echo "$MINIKUBE_IP  academy.local" | sudo tee -a /etc/hosts

# Ahora acceder por hostname
curl http://academy.local/health
open http://academy.local/swagger
```

#### Vía port-forward (alternativa directa)

```bash
# Port-forward de la API
kubectl port-forward svc/academy-api-svc 5000:80 -n academy

# En otro terminal:
curl http://localhost:5000/health
open http://localhost:5000/swagger
```

#### Vía minikube service

```bash
# Abre el servicio en el navegador
minikube service academy-api-svc -n academy --url
```

### 7. Probar el sistema completo en K8s

```bash
# Base URL
BASE="http://academy.local"

# Crear una asignatura
SUBJECT_ID=$(curl -s -X POST $BASE/api/subjects \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Bases de Datos Avanzadas",
    "code": "BDA001",
    "description": "CQRS, Event Sourcing y NoSQL",
    "credits": 6,
    "maxStudents": 30
  }' | jq -r '.id')
echo "Subject ID: $SUBJECT_ID"

# Crear un alumno
STUDENT_ID=$(curl -s -X POST $BASE/api/students \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Carlos",
    "lastName": "Martínez",
    "email": "carlos.martinez@universidad.es",
    "dateOfBirth": "1999-06-20T00:00:00Z"
  }' | jq -r '.id')
echo "Student ID: $STUDENT_ID"

# Matricular al alumno en la asignatura
ENROLLMENT_ID=$(curl -s -X POST $BASE/api/enrollments \
  -H "Content-Type: application/json" \
  -d "{\"studentId\": \"$STUDENT_ID\", \"subjectId\": \"$SUBJECT_ID\"}" \
  | jq -r '.id')
echo "Enrollment ID: $ENROLLMENT_ID"

# Consultar el alumno con sus matrículas (desde MongoDB read DB)
curl -s $BASE/api/students/$STUDENT_ID | jq .

# Listar matrículas del alumno
curl -s $BASE/api/enrollments/student/$STUDENT_ID | jq .
```

### 8. Escalar la API manualmente

```bash
# Escalar a 3 réplicas
kubectl scale deployment academy-api --replicas=3 -n academy

# Verificar HPA
kubectl get hpa -n academy

# Describir el HPA (métricas actuales)
kubectl describe hpa academy-api-hpa -n academy
```

### 9. Depuración útil

```bash
# Shell dentro de un pod de la API
kubectl exec -it -n academy \
  $(kubectl get pod -n academy -l app=academy-api -o jsonpath='{.items[0].metadata.name}') \
  -- /bin/sh

# Acceder a PostgreSQL directamente
kubectl exec -it -n academy \
  $(kubectl get pod -n academy -l app=postgres-write -o jsonpath='{.items[0].metadata.name}') \
  -- psql -U academy -d academy_write

# Acceder a MongoDB directamente
kubectl exec -it -n academy \
  $(kubectl get pod -n academy -l app=mongodb-read -o jsonpath='{.items[0].metadata.name}') \
  -- mongosh academy_read

# Describir un pod para ver eventos de scheduling
kubectl describe pod -n academy -l app=academy-api
```

### 10. Limpiar el entorno K8s

```bash
# Eliminar solo los recursos de la aplicación
kubectl delete namespace academy

# O eliminar recurso a recurso
kubectl delete -f k8s/ --recursive

# Parar Minikube (conserva el estado)
minikube stop

# Eliminar el clúster completamente
minikube delete
```

---

## API Reference

| Método | Endpoint | Descripción | DB |
|--------|----------|-------------|-----|
| `POST` | `/api/students` | Crear alumno | Write (PG) → Read (Mongo) |
| `GET` | `/api/students?page=1&pageSize=20` | Listar alumnos paginado | Read (Mongo) |
| `GET` | `/api/students/{id}` | Obtener alumno por ID | Read (Mongo) |
| `PUT` | `/api/students/{id}` | Actualizar alumno | Write (PG) → Read (Mongo) |
| `DELETE` | `/api/students/{id}` | Eliminar alumno | Write (PG) + Read (Mongo) |
| `POST` | `/api/subjects` | Crear asignatura | Write (PG) → Read (Mongo) |
| `GET` | `/api/subjects` | Listar asignaturas | Read (Mongo) |
| `POST` | `/api/enrollments` | Matricular alumno | Write (PG) → Read (Mongo) |
| `GET` | `/api/enrollments/student/{id}` | Matrículas de un alumno | Read (Mongo) |
| `PUT` | `/api/enrollments/{id}/cancel` | Cancelar matrícula | Write (PG) → Read (Mongo) |
| `GET` | `/health` | Health check | — |

La documentación interactiva completa (Swagger) está disponible en la raíz `/`.

---

## Migraciones EF Core

Las migraciones se aplican automáticamente al iniciar la aplicación (`db.Database.Migrate()`).
Para gestión manual:

```bash
# Instalar herramientas EF Core (una vez)
dotnet tool install --global dotnet-ef

# Desde la raíz del proyecto — crear migración inicial
dotnet ef migrations add InitialCreate \
  --project src/AcademyManager.Infrastructure \
  --startup-project src/AcademyManager.API \
  --context WriteDbContext \
  --output-dir Migrations

# Aplicar migraciones a la BD local
dotnet ef database update \
  --project src/AcademyManager.Infrastructure \
  --startup-project src/AcademyManager.API \
  --context WriteDbContext

# Generar script SQL (útil para revisión)
dotnet ef migrations script \
  --project src/AcademyManager.Infrastructure \
  --startup-project src/AcademyManager.API \
  --context WriteDbContext \
  --output migrations.sql
```

---

## Decisiones de diseño

### Por qué CQRS con dos bases de datos diferentes

El uso de **PostgreSQL para escrituras** y **MongoDB para lecturas** maximiza las ventajas del patrón:

- **Write side (PostgreSQL):** modelo normalizado, integridad referencial, transacciones ACID. Optimizado para la consistencia de datos.
- **Read side (MongoDB):** modelos desnormalizados que devuelven exactamente lo que necesita la UI en una sola consulta, sin JOINs. Optimizado para la velocidad de lectura.

### Por qué Arquitectura Hexagonal

- El **dominio** no conoce ni EF Core, ni MongoDB, ni ASP.NET. Solo define interfaces (puertos).
- La **infraestructura** implementa esos puertos (adaptadores): `StudentRepository` adapta EF Core al puerto `IStudentRepository`.
- El dominio y la aplicación son completamente testeables en memoria, sin necesidad de bases de datos reales.

### Proyecciones via Domain Events

En lugar de doble escritura (escribir en PG y Mongo en el mismo handler), los **Event Handlers de infraestructura** escuchan los Domain Events y actualizan el Read DB de forma reactiva. Esto mantiene el handler de comando enfocado en la escritura y delega la sincronización al event bus interno (MediatR).

### Validación en la capa de aplicación

`FluentValidation` se inyecta como pipeline behavior de MediatR, por lo que los comandos se validan automáticamente antes de llegar al handler, sin código repetitivo en los controladores.
