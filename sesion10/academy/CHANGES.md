# Academy Manager v2 — Guía de cambios y configuraciones

Documento de referencia de todas las modificaciones aplicadas sobre
`academy-manager-tracing` para implementar las mejoras de producción.

---

## Índice

1. [Optimización del Garbage Collector](#1-optimización-del-garbage-collector)
2. [Cabeceras del proxy — Forwarded Headers](#2-cabeceras-del-proxy--forwarded-headers)
3. [Manifiestos Kubernetes — Health Probes, Resources y RollingUpdate](#3-manifiestos-kubernetes--health-probes-resources-y-rollingupdate)
4. [Endpoint de métricas — ThreadPool, HTTP y excepciones](#4-endpoint-de-métricas--threadpool-http-y-excepciones)
5. [API Gateway con YARP](#5-api-gateway-con-yarp)
6. [Bus de eventos — RabbitMQ y MassTransit](#6-bus-de-eventos--rabbitmq-y-masstransit)
7. [Políticas de resiliencia — Retry y Circuit Breaker](#7-políticas-de-resiliencia--retry-y-circuit-breaker)
8. [Transactional Outbox Pattern](#8-transactional-outbox-pattern)
9. [HPA del API Gateway](#9-hpa-del-api-gateway)
10. [Escalado con KEDA](#10-escalado-con-keda)
11. [Flujo completo del sistema](#11-flujo-completo-del-sistema)
12. [Despliegue rápido](#12-despliegue-rápido)

---

## 1. Optimización del Garbage Collector

### Qué es y por qué importa

El GC de .NET tiene dos modos:

| Modo | Cuándo usarlo | Comportamiento |
|---|---|---|
| **Workstation** (defecto) | Escritorio, proceso único | Un hilo de GC, pausas cortas |
| **Server** | Servidor multi-núcleo | Un hilo por CPU, mayor throughput |

En Kubernetes cada Pod dispone de múltiples vCPUs. Sin `ServerGarbageCollection`,
.NET usa solo un hilo para recolectar basura, desaprovechando los núcleos y
aumentando las pausas bajo carga.

### Ficheros modificados

**`src/AcademyManager.API/AcademyManager.API.csproj`**

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>

  <!-- GC en modo servidor: usa un hilo de recolección por CPU disponible.
       Incrementa el throughput en entornos multi-núcleo (Kubernetes nodes). -->
  <ServerGarbageCollection>true</ServerGarbageCollection>

  <!-- Permite al GC ajustar dinámicamente el tamaño del heap según la
       memoria disponible en el contenedor (respeta los limits de K8s). -->
  <GarbageCollectionAdaptationMode>1</GarbageCollectionAdaptationMode>
</PropertyGroup>
```

**`src/AcademyManager.Gateway/AcademyManager.Gateway.csproj`** — misma configuración,
ya que el Gateway también corre en un entorno multi-núcleo y gestiona un alto
volumen de conexiones concurrentes.

### Verificar que está activo en producción

```bash
# Ver el modo GC desde los logs de arranque de .NET
docker compose logs academy-api | grep -i "server gc\|garbage"

# O desde una petición a la API de diagnóstico de .NET (si está habilitada)
curl http://localhost:5000/api/diagnostics/gc

# Estado del GC: confirma que ServerGarbageCollection está activo
curl http://localhost:5000/api/diagnostics/gc

# Estado del ThreadPool: hilos en uso, disponibles y tareas pendientes
curl http://localhost:5000/api/diagnostics/threadpool

# Memoria del proceso: working set, memoria privada y CPU acumulada
curl http://localhost:5000/api/diagnostics/memory

# Resumen de todo en una sola llamada
curl http://localhost:5000/api/diagnostics/summary
```

---

## 2. Cabeceras del proxy — Forwarded Headers

### Qué problema resuelve

En Kubernetes la petición del cliente sigue este camino:

```
Cliente → Ingress (NGINX) → YARP Gateway → academy-api
```

Cada salto reescribe la IP de origen. Sin `UseForwardedHeaders`, la API ve
la IP interna del Gateway en lugar de la IP real del cliente, lo que afecta:

- Logs de acceso (aparece siempre la misma IP interna)
- Trazas de Jaeger/Zipkin (el span no tiene la IP real del cliente)
- Redirects HTTPS (usa `http://` en lugar de `https://` al construir URLs)
- Políticas de rate limiting basadas en IP

### Ficheros modificados

**`src/AcademyManager.API/Program.cs`** y **`src/AcademyManager.Gateway/Program.cs`**

```csharp
// Registrar ANTES de cualquier otro middleware que use la IP o el scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |    // IP real del cliente
        ForwardedHeaders.XForwardedProto;   // Protocolo original (https)

    // Vaciar las listas de redes/proxies de confianza por defecto
    // para aceptar cualquier proxy dentro de la red del clúster.
    // En producción: restringir a las IPs del Ingress Controller.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ...

var app = builder.Build();

// PRIMER middleware del pipeline: debe ir antes de UseRouting,
// UseSerilogRequestLogging y cualquier middleware que lea la IP o el scheme.
app.UseForwardedHeaders();
```

### Verificar que funciona

```bash
# La IP que llega a la API debe ser la del cliente, no la del Gateway
curl -H "X-Forwarded-For: 1.2.3.4" http://localhost:5000/api/students
docker compose logs academy-api | grep "1.2.3.4"
```

---

## 3. Manifiestos Kubernetes — Health Probes, Resources y RollingUpdate

### Las tres sondas de salud

Kubernetes usa tres probes con propósitos distintos. Confundirlas es uno de
los errores más comunes en producción:

| Probe | Endpoint | ¿Qué verifica? | Acción si falla |
|---|---|---|---|
| **Startup** | `/healthz/live` | ¿Ha terminado de arrancar? | Reintentar (no reinicia aún) |
| **Liveness** | `/healthz/live` | ¿Está el proceso vivo? | **Reiniciar el Pod** |
| **Readiness** | `/healthz/ready` | ¿Puede recibir tráfico? | Quitar del balanceador (no reinicia) |

La Readiness probe verifica las dependencias reales (PostgreSQL y RabbitMQ).
Si la BD está caída, el Pod deja de recibir peticiones pero **no se reinicia**,
evitando un bucle de reinicios inútil.

### Endpoints separados en `Program.cs`

```csharp
// Liveness: solo verifica que el proceso está vivo (sin dependencias)
app.MapHealthChecks("/healthz/live", new()
{
    Predicate = _ => false   // no ejecuta checks, responde 200 si el proceso corre
});

// Readiness: verifica PostgreSQL y RabbitMQ (solo checks con tag "ready")
app.MapHealthChecks("/healthz/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});
```

```csharp
// Registrar los checks con el tag "ready" para que solo aparezcan
// en la Readiness probe, no en la Liveness
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WriteDbContext>("postgres-write", tags: new[] { "ready" })
    .AddRabbitMQ(rabbitConnectionString: "...",  name: "rabbitmq", tags: new[] { "ready" })
    .ForwardToPrometheus();
```

### Fichero modificado: `k8s/api/deployment.yaml`

```yaml
containers:
  - name: academy-api
    resources:
      requests:
        memory: "128Mi"   # Mínimo garantizado al Pod
        cpu:    "100m"    # 0.1 vCPU garantizado
      limits:
        memory: "256Mi"   # Máximo permitido (OOMKill si se supera)
        cpu:    "500m"    # 0.5 vCPU máximo

    # Startup: 60 segundos máximo para arrancar (12 intentos × 5s)
    startupProbe:
      httpGet:
        path: /healthz/live
        port: 8080
      initialDelaySeconds: 10
      periodSeconds: 5
      failureThreshold: 12

    # Liveness: reinicia si el proceso se congela
    livenessProbe:
      httpGet:
        path: /healthz/live
        port: 8080
      periodSeconds: 15
      failureThreshold: 3

    # Readiness: quita del balanceador si PostgreSQL o RabbitMQ están caídos
    readinessProbe:
      httpGet:
        path: /healthz/ready
        port: 8080
      periodSeconds: 10
      failureThreshold: 3

strategy:
  type: RollingUpdate
  rollingUpdate:
    maxSurge: 1        # Levanta 1 Pod nuevo antes de bajar el antiguo
    maxUnavailable: 0  # Zero-downtime: nunca hay Pods no disponibles
```

---

## 4. Endpoint de métricas — ThreadPool, HTTP y excepciones

### Métricas expuestas en `/metrics`

Con `prometheus-net >= 6.0`, las siguientes métricas se exponen **automáticamente**
sin escribir código adicional:

**HTTP (peticiones entrantes)**

| Métrica | Descripción |
|---|---|
| `http_requests_received_total` | Total de peticiones por ruta, método y código |
| `http_request_duration_seconds` | Latencia con percentiles P50/P95/P99 |
| `http_requests_in_progress` | Peticiones activas en este momento |

**Runtime .NET 8 — ThreadPool**

| Métrica | Descripción |
|---|---|
| `dotnet_threadpool_threads_total` | Hilos activos en el pool |
| `dotnet_threadpool_queue_length` | Tareas pendientes en la cola |
| `dotnet_threadpool_completed_items_total` | Tareas completadas (tasa) |

**Runtime .NET 8 — Garbage Collector**

| Métrica | Descripción |
|---|---|
| `dotnet_gc_collections_total{generation="0\|1\|2"}` | Colecciones por generación |
| `dotnet_gc_heap_size_bytes` | Tamaño del heap por generación |
| `dotnet_gc_allocated_bytes_total` | Bytes asignados acumulados |

**Health checks como métrica**

| Métrica | Descripción |
|---|---|
| `healthcheck_status{name="postgres-write"}` | 1 = Healthy, 0 = Unhealthy |
| `healthcheck_status{name="rabbitmq"}` | 1 = Healthy, 0 = Unhealthy |

### Configuración en `Program.cs`

```csharp
// Métricas de HttpClient (peticiones salientes)
builder.Services.UseHttpClientMetrics();

// Métricas de ASP.NET Core (peticiones entrantes) + etiqueta custom
app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", _ => "academy-manager");
});

// Exponer el endpoint /metrics que Prometheus hace scrape
app.MapMetrics("/metrics");
```

### Consultas PromQL de referencia

```promql
# Saturación del ThreadPool (debe mantenerse próximo a 0)
dotnet_threadpool_queue_length{app="academy-manager"}

# Tasa de colecciones GC generación 2 (costosas, deben ser infrecuentes)
rate(dotnet_gc_collections_total{generation="2"}[5m])

# Peticiones por segundo a la API
rate(http_requests_received_total{app="academy-manager"}[5m])

# Latencia P95
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Estado de las dependencias (alerta si baja de 1)
healthcheck_status{name="rabbitmq"}
```

---

## 5. API Gateway con YARP

### Por qué YARP y no Ocelot

| Característica | YARP | Ocelot |
|---|---|---|
| Rendimiento | Nativo ASP.NET Core pipeline | Middleware adicional |
| Mantenimiento | Microsoft | Comunidad |
| Health checks activos | Sí (integrado) | Limitado |
| Transformaciones | Fluent API + JSON | JSON |
| .NET 8 soporte oficial | Sí | Parcial |

### Ficheros nuevos

```
src/AcademyManager.Gateway/
├── AcademyManager.Gateway.csproj   ← Yarp.ReverseProxy 2.2.0
├── Program.cs                       ← ForwardedHeaders + YARP + Prometheus
└── appsettings.json                 ← Rutas, clusters y health checks activos
Dockerfile.Gateway                   ← Stage build + runtime multi-stage
k8s/gateway/deployment.yaml          ← Deployment + Service + HPA
```

### Rutas configuradas en `appsettings.json`

```json
"ReverseProxy": {
  "Routes": {
    "students-route":    { "ClusterId": "academy-api-cluster", "Match": { "Path": "/api/students/{**catch-all}" } },
    "subjects-route":    { "ClusterId": "academy-api-cluster", "Match": { "Path": "/api/subjects/{**catch-all}" } },
    "enrollments-route": { "ClusterId": "academy-api-cluster", "Match": { "Path": "/api/enrollments/{**catch-all}" } }
  },
  "Clusters": {
    "academy-api-cluster": {
      "HealthCheck": {
        "Active": {
          "Enabled": true,
          "Interval": "00:00:10",
          "Path": "/healthz/ready"    ← comprueba que la API puede recibir tráfico
        }
      },
      "LoadBalancingPolicy": "RoundRobin",
      "Destinations": {
        "academy-api-1": { "Address": "http://academy-api:8080" }
      }
    }
  }
}
```

### Puertos

| Servicio | Puerto local | Descripción |
|---|---|---|
| `academy-gateway` | `8090` | Punto de entrada único para los clientes |
| `academy-api` | `5000` | Acceso directo (dev/debug) |

### Transformaciones de cabeceras

El Gateway añade automáticamente `X-Gateway: academy-gateway` en cada petición
reenviada, permitiendo a los microservicios saber que la petición pasó por el
proxy y auditar el origen en los logs.

---

## 6. Bus de eventos — RabbitMQ y MassTransit

### Flujo de datos con EDA

Sin bus de eventos (acoplamiento síncrono):

```
API → PostgreSQL → HTTP → MongoDB    ← si MongoDB está caído, la escritura falla
```

Con RabbitMQ + MassTransit (desacoplamiento asíncrono):

```
API → PostgreSQL + Outbox → RabbitMQ → Consumer → MongoDB
         (misma transacción)                 ↑ si falla, se reintenta
```

### Ficheros modificados

**`src/AcademyManager.Infrastructure/AcademyManager.Infrastructure.csproj`**

```xml
<PackageReference Include="MassTransit"                  Version="8.2.3" />
<PackageReference Include="MassTransit.RabbitMQ"         Version="8.2.3" />
<PackageReference Include="MassTransit.EntityFrameworkCore" Version="8.2.3" />
```

**`src/AcademyManager.Infrastructure/Messaging/Consumers.cs`** — fichero nuevo

Define los contratos de eventos y el consumer que actualiza MongoDB:

```csharp
// Contrato del evento que viaja por RabbitMQ
public record AlumnoMatriculadoEvent(
    Guid EnrollmentId, Guid StudentId, Guid SubjectId,
    string StudentName, string SubjectName, string SubjectCode,
    DateTime EnrolledAt);

// Consumer: se ejecuta cuando MassTransit entrega el evento
public sealed class AlumnoMatriculadoConsumer : IConsumer<AlumnoMatriculadoEvent>
{
    public async Task Consume(ConsumeContext<AlumnoMatriculadoEvent> context)
    {
        // 1. Upsert en la colección enrollments de MongoDB
        // 2. Añadir resumen al documento del estudiante
        // 3. Incrementar el contador de la asignatura
    }
}
```

**`docker-compose.yml`** — servicio RabbitMQ añadido

```yaml
rabbitmq:
  image: rabbitmq:3.13-management-alpine
  ports:
    - "5672:5672"    # AMQP — conexiones de MassTransit
    - "15672:15672"  # Management UI → http://localhost:15672 (guest/guest)
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "check_port_connectivity"]
```

**`k8s/rabbitmq/rabbitmq.yaml`** — fichero nuevo con Deployment + Service + PVC

---

## 7. Políticas de resiliencia — Retry y Circuit Breaker

### Por qué son necesarias

En un sistema distribuido los fallos transitorios son inevitables:

- Una query a MongoDB puede fallar por un pico de carga puntual → **Retry**
- MongoDB puede estar caído varios minutos → **Circuit Breaker** (para de intentarlo)

Sin estas políticas, un consumer que falla reprocesa infinitamente el mismo mensaje
hasta llenarse el Dead Letter Queue y saturar el sistema.

### Configuración en `DependencyInjection.cs`

```csharp
x.AddConsumer<AlumnoMatriculadoConsumer>(cfg =>
{
    // Retry incremental: reintenta 3 veces esperando 1s, 2s, 3s.
    // Si los 3 reintentos fallan, el mensaje va a la Dead Letter Queue.
    cfg.UseMessageRetry(r => r.Incremental(
        retryLimit:       3,
        initialInterval:  TimeSpan.FromSeconds(1),
        intervalIncrement: TimeSpan.FromSeconds(1)));

    // Circuit Breaker: si hay 5 excepciones en 60 segundos, el circuito
    // se abre durante 30 segundos. Durante ese tiempo los mensajes no se
    // intentan procesar (evita saturar un sistema ya degradado).
    cfg.UseCircuitBreaker(cb =>
    {
        cb.TrackingPeriod = TimeSpan.FromSeconds(60);
        cb.TripThreshold  = 5;     // % de fallos para abrir
        cb.ActiveThreshold = 5;    // mínimo de mensajes antes de evaluar
        cb.ResetInterval  = TimeSpan.FromSeconds(30);
    });
});
```

### Estados del Circuit Breaker

```
Closed (normal) ──5 fallos en 60s──► Open (pausa 30s) ──reset──► Half-Open ──OK──► Closed
                                          │                            │
                                     mensajes no               prueba con 1 mensaje
                                     se procesan               si falla → vuelve a Open
```

---

## 8. Transactional Outbox Pattern

### El problema que resuelve

El mayor error de diseño en EDA es publicar un evento en RabbitMQ en medio
de una transacción de base de datos:

```csharp
// ❌ MAL: si la red con RabbitMQ falla después del Commit, el evento se pierde
await dbContext.SaveChangesAsync();           // PostgreSQL confirma
await bus.Publish(new AlumnoMatriculadoEvent()); // RabbitMQ puede fallar aquí
```

```csharp
// ✅ BIEN con Outbox: el evento se guarda en PostgreSQL junto con el dato
// en la misma transacción. Un Worker lo envía a RabbitMQ de forma segura.
await dbContext.SaveChangesAsync();  // Guarda dato + evento Outbox en 1 transacción
// El Worker lee el Outbox y publica → At-least-once delivery garantizado
```

### Configuración en `DependencyInjection.cs`

```csharp
x.AddEntityFrameworkOutbox<WriteDbContext>(o =>
{
    // Usa PostgreSQL como almacén del Outbox (tablas OutboxMessage y OutboxState)
    o.UsePostgres();

    // Sincroniza la publicación del evento con la transacción EF Core.
    // El Worker en segundo plano lee las tablas Outbox y publica en RabbitMQ.
    o.UseBusOutbox();
});
```

### Tablas creadas automáticamente en PostgreSQL

| Tabla | Propósito |
|---|---|
| `OutboxMessage` | Almacena los eventos pendientes de publicar |
| `OutboxState` | Estado de procesamiento del Worker por consumer group |

### Verificar el Outbox en acción

```bash
# Ver mensajes pendientes en el Outbox (deben estar en 0 si el sistema está sano)
docker compose exec postgres-write psql -U academy -d academy_write \
  -c "SELECT COUNT(*), delivered_at IS NULL as pending FROM \"OutboxMessage\" GROUP BY 2;"

# Ver la cola en RabbitMQ Management UI
open http://localhost:15672
```

---

## 9. HPA del API Gateway

### Por qué el Gateway necesita HPA propio

El Gateway es **stateless** (no guarda sesiones ni estado entre peticiones).
Esto lo hace ideal para el escalado horizontal: cualquier número de réplicas
puede atender cualquier petición sin coordinación entre ellas.

### Fichero modificado: `k8s/gateway/deployment.yaml`

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: academy-gateway-hpa
  namespace: academy
spec:
  scaleTargetRef:
    kind: Deployment
    name: academy-gateway
  minReplicas: 2
  maxReplicas: 8
  metrics:
    # Escala cuando el CPU medio supera el 70%
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    # Escala también por memoria al 75%
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 75
  behavior:
    scaleUp:
      stabilizationWindowSeconds: 30   # reacciona rápido ante picos
      policies:
        - type: Pods
          value: 2
          periodSeconds: 60
    scaleDown:
      stabilizationWindowSeconds: 120  # espera antes de reducir (evita flapping)
      policies:
        - type: Pods
          value: 1
          periodSeconds: 60
```

### Verificar el escalado

```bash
# Ver el estado actual del HPA
kubectl get hpa -n academy

# Simular carga para disparar el escalado
kubectl run load-test --rm -it --image=busybox:1.36 -- \
  sh -c "while true; do wget -q -O- http://academy-gateway-svc/api/students; done"

# Ver cómo escalan los Pods en tiempo real
kubectl get pods -n academy -l app=academy-gateway -w
```

---

## 10. Escalado con KEDA

### Por qué KEDA y no el HPA para los consumers

El HPA tradicional escala por CPU/memoria. En el flujo de matriculaciones:

```
1. Llegan 1000 matriculaciones en 10 segundos
2. La cola de RabbitMQ crece a 1000 mensajes
3. El consumer empieza a procesarlos → CPU sube
4. HPA detecta el aumento de CPU (tras ~30-60s de lag)
5. Se crean nuevos Pods (pero ya hay 800 mensajes acumulados)
```

Con KEDA:

```
1. Llegan 1000 matriculaciones en 10 segundos
2. La cola crece a 1000 mensajes
3. KEDA detecta la cola en 10 segundos → crea inmediatamente 50 Pods
4. Los 1000 mensajes se procesan en paralelo
```

### Prerequisito — Instalar KEDA en el clúster

```bash
helm repo add kedacore https://kedacore.github.io/charts
helm repo update
helm install keda kedacore/keda \
  --namespace keda \
  --create-namespace
```

### Fichero nuevo: `k8s/keda/scaledobject.yaml`

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: academy-api-scaler
  namespace: academy
spec:
  scaleTargetRef:
    kind: Deployment
    name: academy-api
  minReplicaCount: 1     # cambiar a 0 para Scale-to-Zero
  maxReplicaCount: 10
  cooldownPeriod:  60    # segundos antes de reducir réplicas
  pollingInterval: 10    # comprueba la cola cada 10 segundos
  triggers:
    - type: rabbitmq
      metadata:
        queueName:   AlumnoMatriculadoEvent
        queueLength: "20"   # 1 Pod nuevo por cada 20 mensajes en cola
        protocol:    http   # usa la Management API de RabbitMQ
```

### Verificar KEDA en acción

```bash
# Ver el estado del ScaledObject
kubectl get scaledobject -n academy

# Ver el número actual de réplicas y el trigger value
kubectl describe scaledobject academy-api-scaler -n academy

# Generar carga para disparar el escalado
for i in {1..200}; do
  curl -s -X POST http://localhost:8090/api/students \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"Alumno $i\",\"email\":\"alumno$i@test.com\",\"enrollmentNumber\":\"K00$i\"}"
done

# Ver cómo KEDA escala los Pods
kubectl get pods -n academy -l app=academy-api -w
```

---

## 11. Flujo completo del sistema

```
                         ┌─────────────────────────────────────────────────┐
                         │              academy-network                     │
                         │                                                   │
  Cliente ──HTTP──────►  │  YARP Gateway :8090                              │
                         │    ├── /api/students   ─────┐                   │
                         │    ├── /api/subjects   ─────┤                   │
                         │    └── /api/enrollments ────┤                   │
                         │                             ▼                   │
                         │                    academy-api :8080             │
                         │                         │                        │
                         │               ┌─────────┴──────────┐            │
                         │               │                    │            │
                         │          PostgreSQL           MongoDB            │
                         │          (write side)         (read side)        │
                         │               │                    ▲            │
                         │          Outbox Tables             │            │
                         │               │                    │            │
                         │               ▼                    │            │
                         │          RabbitMQ ─── Consumer ───►│            │
                         │               │       (KEDA escala              │
                         │               │        según cola)              │
                         │               │                                  │
                         │  Jaeger/Zipkin ◄── trazas OTLP ──────────────── │
                         │  Prometheus   ◄── /metrics  ─────────────────── │
                         │  Grafana      ◄── dashboards ─────────────────  │
                         └─────────────────────────────────────────────────┘
```

**Pasos detallados:**

1. El cliente envía una petición a **YARP Gateway** (`:8090`)
2. El Gateway verifica con el Health Check activo que `academy-api` está en Readiness y enruta la petición
3. La API ejecuta el Command → Handler escribe en **PostgreSQL**
4. El evento de dominio se guarda en las tablas **Outbox** dentro de la misma transacción
5. El Worker de MassTransit lee el Outbox y publica en **RabbitMQ**
6. Si la cola supera 20 mensajes, **KEDA** escala horizontalmente los Pods del consumer
7. El `AlumnoMatriculadoConsumer` procesa el mensaje y actualiza **MongoDB**
8. Las trazas de todos los pasos llegan a **Jaeger** y/o **Zipkin** vía OpenTelemetry
9. Las métricas (HTTP, ThreadPool, GC, Healthchecks) llegan a **Prometheus** y se visualizan en **Grafana**

---

## 12. Despliegue rápido

### Docker Compose (desarrollo local)

```bash
# Levantar todo el stack
docker compose up --build -d

# Verificar que todos los servicios están corriendo
docker compose ps

# Ver logs en tiempo real
docker compose logs -f academy-api academy-gateway
```

| Servicio | URL |
|---|---|
| YARP Gateway | http://localhost:8090 |
| API directa + Swagger | http://localhost:5000 |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |
| Jaeger UI | http://localhost:16686 |
| Zipkin UI | http://localhost:9411 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 (admin/admin123) |

### Kubernetes con Minikube

```bash
# 1. Arrancar Minikube
minikube start --driver=docker --cpus=4 --memory=6144
minikube addons enable ingress metrics-server

# 2. Instalar KEDA
helm repo add kedacore https://kedacore.github.io/charts && helm repo update
helm install keda kedacore/keda --namespace keda --create-namespace

# 3. Instalar Prometheus + Grafana
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install monitoring prometheus-community/kube-prometheus-stack \
  --namespace monitoring --create-namespace \
  --set grafana.adminPassword=admin123

# 4. Construir imágenes dentro de Minikube
eval $(minikube docker-env)
docker build -t academy-manager-api:latest .
docker build -t academy-gateway:latest -f Dockerfile.Gateway .

# 5. Aplicar todos los manifiestos
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secrets/
kubectl apply -f k8s/configmaps/
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/mongodb/
kubectl apply -f k8s/rabbitmq/
kubectl apply -f k8s/tracing/
kubectl apply -f k8s/api/
kubectl apply -f k8s/gateway/
kubectl apply -f k8s/keda/
kubectl apply -f k8s/ingress/

# 6. Verificar que todo está Running
kubectl get pods -n academy

# 7. Port-forwards para acceder
kubectl port-forward svc/academy-gateway-svc 8090:80 -n academy &
kubectl port-forward svc/jaeger-svc          16686:16686 -n academy &
kubectl port-forward svc/rabbitmq-svc        15672:15672 -n academy &
kubectl port-forward svc/monitoring-grafana  3000:80 -n monitoring &
```

### Cambiar el backend de trazabilidad sin reconstruir

```bash
# Solo Zipkin
kubectl set env deployment/academy-api Tracing__Backend=zipkin -n academy

# Ambos a la vez
kubectl set env deployment/academy-api Tracing__Backend=both -n academy

# Confirmar el rollout
kubectl rollout status deployment/academy-api -n academy
```

---

> **Estructura del proyecto**
>
> ```
> academy-manager-v2/
> ├── src/
> │   ├── AcademyManager.Domain/
> │   ├── AcademyManager.Application/
> │   ├── AcademyManager.Infrastructure/
> │   │   └── Messaging/Consumers.cs         ← NUEVO
> │   ├── AcademyManager.API/
> │   └── AcademyManager.Gateway/            ← NUEVO (YARP)
> ├── k8s/
> │   ├── api/deployment.yaml                ← MODIFICADO (3 probes + RabbitMQ)
> │   ├── gateway/deployment.yaml            ← NUEVO (HPA)
> │   ├── rabbitmq/rabbitmq.yaml             ← NUEVO
> │   ├── keda/scaledobject.yaml             ← NUEVO
> │   └── tracing/ (Jaeger + Zipkin)
> ├── monitoring/prometheus.yml              ← MODIFICADO (job gateway)
> ├── docker-compose.yml                     ← MODIFICADO (RabbitMQ + Gateway)
> ├── Dockerfile
> ├── Dockerfile.Gateway                     ← NUEVO
> └── CHANGES.md                             ← ESTE FICHERO
> ```
