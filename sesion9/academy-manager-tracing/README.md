# Academy Manager — CQRS + Hexagonal + Observabilidad + Trazabilidad distribuida

Proyecto de demostración con **CQRS**, **Arquitectura Hexagonal**, stack de observabilidad con **Prometheus** y **Grafana**, y trazabilidad distribuida con **Jaeger** y **Zipkin** mediante **OpenTelemetry**.

---

## Tabla de contenidos

1. [Arquitectura](#arquitectura)
2. [Stack de servicios](#stack-de-servicios)
3. [Despliegue con Docker Compose](#despliegue-con-docker-compose)
4. [Acceso a Prometheus](#acceso-a-prometheus)
5. [Acceso a Grafana](#acceso-a-grafana)
6. [Trazabilidad distribuida](#trazabilidad-distribuida)
   - [Activar Jaeger](#activar-jaeger-por-defecto)
   - [Activar Zipkin](#activar-zipkin)
   - [Activar Jaeger y Zipkin a la vez](#activar-jaeger-y-zipkin-a-la-vez)
7. [Cómo cambiar de backend sin reconstruir](#cómo-cambiar-de-backend-sin-reconstruir)
8. [Usar la UI de Jaeger](#usar-la-ui-de-jaeger)
9. [Usar la UI de Zipkin](#usar-la-ui-de-zipkin)
10. [API REST de Jaeger y Zipkin](#api-rest-de-jaeger-y-zipkin)
11. [Métricas disponibles](#métricas-disponibles)
12. [Despliegue en Kubernetes con Minikube](#despliegue-en-kubernetes-con-minikube)

---

## Arquitectura

```
                    ┌──────────────────────────────────────────────────────┐
                    │                  academy-network                      │
                    │                                                        │
  HTTP ────────────▶│  academy-api :8080                                    │
                    │    /metrics  ──────────────────────▶ prometheus :9090 │
                    │    /health                                    │        │
                    │    /api/...                            grafana :3000   │
                    │       │                                               │
                    │       ├── trazas OTLP gRPC ──────────▶ jaeger :4317  │
                    │       └── trazas HTTP      ──────────▶ zipkin :9411  │
                    │              (uno, otro, o ambos según Tracing__Backend)
                    │       │                                               │
                    │  ┌────┴────┐  ┌──────────────┐                       │
                    │  │postgres │  │   mongodb    │                       │
                    │  │  write  │  │    read      │                       │
                    │  │  :5432  │  │   :27017     │                       │
                    │  └────┬────┘  └──────┬───────┘                       │
                    │  pg-exp:9187  mongo-exp:9216  node-exp:9100           │
                    └──────────────────────────────────────────────────────┘
```

---

## Stack de servicios

| Servicio | Puerto | Descripción |
|---|---|---|
| `academy-api` | 5000 | API REST — Swagger en `http://localhost:5000` |
| `postgres-write` | 5432 | PostgreSQL — write side (CQRS) |
| `mongodb-read` | 27017 | MongoDB — read side (CQRS) |
| `postgres-exporter` | 9187 | Exporta métricas de PostgreSQL a Prometheus |
| `mongodb-exporter` | 9216 | Exporta métricas de MongoDB a Prometheus |
| `node-exporter` | 9100 | Métricas de CPU/RAM/disco del host |
| `prometheus` | 9090 | Recolecta y almacena series temporales |
| `grafana` | 3000 | Dashboards de visualización |
| `jaeger` | 16686 / 4317 / 4318 | UI de trazas + receptor OTLP gRPC/HTTP |
| `zipkin` | 9411 | UI de trazas + API REST |
| `mongo-express` | 8081 | UI para MongoDB (perfil `tools`, opcional) |

---

## Despliegue con Docker Compose

```bash
docker compose up --build -d
docker compose ps
```

Parar y limpiar:

```bash
docker compose down        # conserva datos
docker compose down -v     # borra también los volúmenes
```

---

## Acceso a Prometheus

**URL:** http://localhost:9090

```promql
rate(http_requests_received_total{job="academy-api"}[5m])
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
rate(http_requests_received_total{code=~"5.."}[5m])
```

Verificar targets: http://localhost:9090/targets

---

## Acceso a Grafana

**URL:** http://localhost:3000 — `admin` / `admin123`

| Dashboard ID | Nombre |
|---|---|
| 10427 | ASP.NET Core (`prometheus-net`) |
| 9628 | PostgreSQL |
| 2583 | MongoDB |
| 1860 | Node Exporter Full |

---

## Trazabilidad distribuida

La variable de entorno `Tracing__Backend` controla qué backends reciben las trazas.
Admite tres valores y **no requiere reconstruir la imagen** para cambiarlo.

| Valor | Resultado |
|---|---|
| `jaeger` | Solo Jaeger recibe trazas (valor por defecto) |
| `zipkin` | Solo Zipkin recibe trazas |
| `both` | Jaeger **y** Zipkin reciben trazas simultáneamente |

En los tres casos se auto-instrumenta:
- Peticiones HTTP entrantes a la API (excluye `/metrics` y `/health`)
- Peticiones HTTP salientes vía `HttpClient`
- Queries SQL de Entity Framework Core (con el texto SQL incluido en el span)

### Activar Jaeger (por defecto)

En `docker-compose.yml` la variable ya viene configurada:

```yaml
Tracing__Backend: "jaeger"
```

Levantar el stack:

```bash
docker compose up --build -d
```

Verificar en los logs que Jaeger está activo:

```bash
docker compose logs academy-api | grep Tracing
# [Tracing] Backend activo: Jaeger
```

### Activar Zipkin

**Opción A — editar `docker-compose.yml`** (el cambio persiste):

```yaml
Tracing__Backend: "zipkin"   # ← cambiar esta línea
```

```bash
docker compose up -d academy-api   # solo reinicia la API, no el resto
```

**Opción B — variable inline** (sin tocar el fichero, efecto temporal):

```bash
Tracing__Backend=zipkin docker compose up -d academy-api
```

**Opción C — `docker compose run`** (útil para pruebas puntuales):

```bash
docker compose run --rm -e Tracing__Backend=zipkin academy-api
```

Verificar en los logs:

```bash
docker compose logs academy-api | grep Tracing
# [Tracing] Backend activo: Zipkin
```

### Activar Jaeger y Zipkin a la vez

```yaml
Tracing__Backend: "both"
```

```bash
docker compose up -d academy-api
docker compose logs academy-api | grep Tracing
# [Tracing] Backend activo: Jaeger + Zipkin
```

Las mismas trazas llegarán a ambas UIs simultáneamente, lo que permite comparar
cómo presenta cada herramienta la misma información.

---

## Cómo cambiar de backend sin reconstruir

La imagen Docker no cambia. OpenTelemetry lee `Tracing__Backend` en tiempo de arranque.
El flujo completo en tres pasos:

```bash
# 1. Editar la variable en docker-compose.yml (o exportarla inline)
#    Tracing__Backend: "zipkin"  |  "jaeger"  |  "both"

# 2. Reiniciar solo la API (el resto del stack sigue funcionando)
docker compose up -d academy-api

# 3. Confirmar el backend activo
docker compose logs academy-api --tail=20 | grep Tracing
```

En Kubernetes:

```bash
# Cambiar a Zipkin
kubectl set env deployment/academy-api Tracing__Backend=zipkin -n academy

# Cambiar a ambos
kubectl set env deployment/academy-api Tracing__Backend=both -n academy

# Volver a Jaeger
kubectl set env deployment/academy-api Tracing__Backend=jaeger -n academy

# Verificar el rollout
kubectl rollout status deployment/academy-api -n academy
```

---

## Usar la UI de Jaeger

**URL:** http://localhost:16686

### Generar trazas

```bash
# Petición de lectura (traza: HTTP → MongoDB)
curl -s http://localhost:5000/api/students

# Petición de escritura (traza: HTTP → EF Core → PostgreSQL)
curl -s -X POST http://localhost:5000/api/students \
  -H "Content-Type: application/json" \
  -d '{"name":"Ana García","email":"ana@academy.com","enrollmentNumber":"A001"}'

# Bucle para generar varias trazas
for i in {1..20}; do
  curl -s http://localhost:5000/api/students > /dev/null
  sleep 0.3
done
```

### Buscar trazas en la UI

1. Seleccionar **Service** → `academy-manager`
2. Seleccionar **Operation** → la ruta que quieras inspeccionar
3. Pulsar **Find Traces**
4. Clicar en una traza para ver el gráfico de Gantt con todos los spans

### Comparar dos trazas

Seleccionar dos trazas en la lista y pulsar **Compare** para ver en qué spans
difirió el rendimiento entre dos ejecuciones.

---

## Usar la UI de Zipkin

**URL:** http://localhost:9411

### Generar trazas (asegúrate de que `Tracing__Backend` es `zipkin` o `both`)

```bash
curl -s http://localhost:5000/api/students
curl -s http://localhost:5000/api/subjects
```

### Buscar trazas en la UI

1. Pulsar **Run Query** para ver todas las trazas recientes
2. Filtrar por **serviceName** → `academy-manager`
3. Clicar en una traza para ver los spans
4. Ir a **Dependencies** para ver el mapa de servicios generado automáticamente

---

## API REST de Jaeger y Zipkin

### Jaeger — puerto 16686

```bash
# Servicios registrados
curl http://localhost:16686/api/services

# Operaciones de un servicio
curl "http://localhost:16686/api/operations?service=academy-manager"

# Últimas 20 trazas
curl "http://localhost:16686/api/traces?service=academy-manager&limit=20"

# Filtrar por operación
curl "http://localhost:16686/api/traces?service=academy-manager&operation=GET%20%2Fapi%2Fstudents"

# Trazas con duración mínima de 50 ms (en microsegundos)
curl "http://localhost:16686/api/traces?service=academy-manager&minDuration=50000"

# Traza concreta por ID
curl "http://localhost:16686/api/traces/{TRACE_ID}"

# Dependencias entre servicios
curl "http://localhost:16686/api/dependencies?endTs=$(date +%s)000&lookback=3600000"
```

| Parámetro | Descripción |
|---|---|
| `service` | Nombre del servicio (requerido) |
| `operation` | Nombre de la ruta/operación |
| `limit` | Número máximo de trazas (default: 20) |
| `minDuration` | Duración mínima en microsegundos |
| `maxDuration` | Duración máxima en microsegundos |
| `tags` | Filtro por tags (`http.status_code=500`) |

### Zipkin — puerto 9411

```bash
# Servicios registrados
curl http://localhost:9411/api/v2/services

# Spans de un servicio
curl "http://localhost:9411/api/v2/spans?serviceName=academy-manager"

# Últimas 10 trazas
curl "http://localhost:9411/api/v2/traces?serviceName=academy-manager&limit=10"

# Filtrar por nombre de span
curl "http://localhost:9411/api/v2/traces?serviceName=academy-manager&spanName=get+%2Fapi%2Fstudents"

# Trazas con duración mínima de 50 ms
curl "http://localhost:9411/api/v2/traces?serviceName=academy-manager&minDuration=50000"

# Traza concreta por ID
curl "http://localhost:9411/api/v2/trace/{TRACE_ID}"

# Dependencias (última hora)
curl "http://localhost:9411/api/v2/dependencies?endTs=$(date +%s)000&lookback=3600000"

# Enviar un span manualmente (útil para pruebas)
curl -X POST http://localhost:9411/api/v2/spans \
  -H "Content-Type: application/json" \
  -d '[{
    "traceId": "aabbccdd00112233",
    "id": "aabbccdd00112233",
    "name": "test-span",
    "timestamp": '"$(date +%s)"'000000,
    "duration": 15000,
    "localEndpoint": {"serviceName": "academy-manager"},
    "tags": {"http.method": "GET", "http.status_code": "200"}
  }]'
```

| Parámetro | Descripción |
|---|---|
| `serviceName` | Nombre del servicio |
| `spanName` | Nombre del span/operación |
| `minDuration` | Duración mínima en microsegundos |
| `maxDuration` | Duración máxima en microsegundos |
| `endTs` | Timestamp de fin en ms (Unix epoch) |
| `lookback` | Ventana hacia atrás en ms (3600000 = 1h) |
| `limit` | Número máximo de trazas (default: 10) |

---

## Métricas disponibles

### HTTP (prometheus-net) — `http://localhost:5000/metrics`

| Métrica | Tipo | Descripción |
|---|---|---|
| `http_requests_received_total` | Counter | Peticiones por método, ruta y código HTTP |
| `http_request_duration_seconds` | Histogram | Latencia por ruta |
| `http_requests_in_progress` | Gauge | Peticiones activas en este momento |

### Runtime .NET

| Métrica | Descripción |
|---|---|
| `dotnet_gc_heap_size_bytes` | Heap GC por generación |
| `dotnet_gc_collections_total` | Colecciones GC por generación |
| `dotnet_threadpool_threads_total` | Hilos del thread pool |
| `process_cpu_seconds_total` | CPU consumida |
| `process_working_set_bytes` | RAM usada por el proceso |

### Health check

| Métrica | Descripción |
|---|---|
| `healthcheck_status{name="postgres-write"}` | 1 = Healthy, 0 = Unhealthy |

---

## Estructura del proyecto

```
academy-manager/
├── src/
│   ├── AcademyManager.API/
│   │   ├── Program.cs              # OpenTelemetry: jaeger | zipkin | both
│   │   ├── AcademyManager.API.csproj
│   │   └── ...
│   ├── AcademyManager.Application/
│   ├── AcademyManager.Infrastructure/
│   └── AcademyManager.Domain/
├── monitoring/
│   ├── prometheus.yml
│   └── grafana/
├── k8s/
│   ├── namespace.yaml
│   ├── secrets/
│   ├── configmaps/
│   ├── postgres/
│   ├── mongodb/
│   ├── api/
│   │   └── deployment.yaml        # Variables Tracing__ + initContainer jaeger
│   ├── ingress/
│   └── tracing/
│       ├── jaeger.yaml            # Deployment + Service Jaeger
│       └── zipkin.yaml            # Deployment + Service Zipkin
├── scripts/
├── docker-compose.yml
├── Dockerfile
└── README.md
```

---

## Despliegue en Kubernetes con Minikube

### Paso 1 — Iniciar Minikube

```bash
minikube start --driver=docker --cpus=4 --memory=4096
minikube addons enable ingress
minikube addons enable metrics-server
```

### Paso 2 — Construir y cargar la imagen

```bash
eval $(minikube docker-env)
docker build -t academy-manager-api:latest .
```

### Paso 3 — Aplicar manifiestos

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secrets/
kubectl apply -f k8s/configmaps/
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/mongodb/
kubectl apply -f k8s/tracing/       # ← Jaeger y Zipkin
kubectl apply -f k8s/api/
```

### Paso 4 — Instalar Prometheus y Grafana con Helm

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm install monitoring prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --set grafana.adminPassword=admin123
```

### Paso 5 — ServiceMonitor para la API

```bash
cat <<EOF | kubectl apply -f -
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: academy-api-monitor
  namespace: monitoring
  labels:
    release: monitoring
spec:
  namespaceSelector:
    matchNames:
      - academy
  selector:
    matchLabels:
      app: academy-api
  endpoints:
    - port: http
      path: /metrics
      interval: 10s
EOF
```

### Paso 6 — Port-forwards para acceder a los servicios

```bash
# Jaeger UI
kubectl port-forward svc/jaeger-svc 16686:16686 -n academy &
# http://localhost:16686

# Zipkin UI
kubectl port-forward svc/zipkin-svc 9411:9411 -n academy &
# http://localhost:9411

# API
kubectl port-forward svc/academy-api-svc 5000:80 -n academy &
# http://localhost:5000

# Grafana
kubectl port-forward svc/monitoring-grafana 3000:80 -n monitoring &
# http://localhost:3000  (admin / admin123)
```

### Paso 7 — Cambiar backend de tracing en Kubernetes

```bash
# Solo Zipkin
kubectl set env deployment/academy-api Tracing__Backend=zipkin -n academy

# Ambos a la vez
kubectl set env deployment/academy-api Tracing__Backend=both -n academy

# Volver a Jaeger
kubectl set env deployment/academy-api Tracing__Backend=jaeger -n academy

kubectl rollout status deployment/academy-api -n academy
```

---

> **Tip — Recargar Prometheus sin reiniciar:**
> ```bash
> curl -X POST http://localhost:9090/-/reload
> ```
