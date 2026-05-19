# Academy Manager — CQRS + Hexagonal Architecture + Observabilidad

Proyecto de demostración con **CQRS**, **Arquitectura Hexagonal**, y stack de observabilidad completo con **Prometheus** y **Grafana**.

---

## Tabla de contenidos

1. [Arquitectura](#arquitectura)
2. [Stack de servicios](#stack-de-servicios)
3. [Despliegue con Docker Compose](#despliegue-con-docker-compose)
4. [Acceso a Prometheus](#acceso-a-prometheus)
5. [Acceso a Grafana](#acceso-a-grafana)
6. [Métricas disponibles](#métricas-disponibles)
7. [Despliegue en Kubernetes con Minikube](#despliegue-en-kubernetes-con-minikube)

---

## Arquitectura

```
                         ┌─────────────────────────────────┐
                         │        academy-network           │
                         │                                  │
   Petición HTTP ───────▶│  academy-api :8080               │
                         │    /metrics  ──────────────────▶ prometheus :9090
                         │    /health                              │
                         │    /api/...                      grafana :3000 ◀── navegador
                         │       │                                 │
                         │  ┌────┴────┐  ┌────────────────┐       │
                         │  │postgres │  │   mongodb      │       │
                         │  │  write  │  │    read        │       │
                         │  │  :5432  │  │   :27017       │       │
                         │  └────┬────┘  └────────┬───────┘       │
                         │       │                │               │
                         │  pg-exporter:9187  mongo-exporter:9216 │
                         │       └────────────────┘               │
                         │               scraping ────────────────┘
                         └─────────────────────────────────────────┘
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
| `prometheus` | 9090 | Recolecta y almacena series temporales |
| `grafana` | 3000 | Dashboards de visualización |
| `mongo-express` | 8081 | UI para MongoDB (perfil `tools`, opcional) |

---

## Despliegue con Docker Compose

### 1. Construir e iniciar todos los servicios

```bash
docker compose up --build
```

En segundo plano:

```bash
docker compose up --build -d
```

### 2. Verificar que todos los servicios están healthy

```bash
docker compose ps
```

Salida esperada:

```
NAME                      STATUS          PORTS
academy-api               Up (healthy)    0.0.0.0:5000->8080/tcp
academy-postgres-write    Up (healthy)    0.0.0.0:5432->5432/tcp
academy-mongodb-read      Up (healthy)    0.0.0.0:27017->27017/tcp
academy-postgres-exporter Up              0.0.0.0:9187->9187/tcp
academy-mongodb-exporter  Up              0.0.0.0:9216->9216/tcp
academy-prometheus        Up              0.0.0.0:9090->9090/tcp
academy-grafana           Up              0.0.0.0:3000->3000/tcp
```

### 3. Parar y limpiar

```bash
# Parar sin borrar datos
docker compose down

# Parar y borrar todos los volúmenes (datos incluidos)
docker compose down -v
```

---

## Acceso a Prometheus

**URL:** http://localhost:9090

Prometheus recoge métricas de cuatro fuentes cada 15 segundos (10s para la API):

| Job | Target | Qué mide |
|---|---|---|
| `academy-api` | `academy-api:8080/metrics` | Peticiones HTTP, latencia, runtime .NET |
| `postgres` | `postgres-exporter:9187` | Conexiones, queries, locks, tamaño DB |
| `mongodb` | `mongodb-exporter:9216` | Operaciones, conexiones, memoria |
| `prometheus` | `localhost:9090` | Auto-monitorización |

### Consultas de ejemplo en Prometheus

Pega estas PromQL en el campo **Expression** de http://localhost:9090/graph:

```promql
# Peticiones por segundo a la API (últimos 5 min)
rate(http_requests_received_total{job="academy-api"}[5m])

# Latencia P95 de todos los endpoints
histogram_quantile(0.95,
  rate(http_request_duration_seconds_bucket{job="academy-api"}[5m])
)

# Tasa de errores HTTP 5xx
rate(http_requests_received_total{job="academy-api", code=~"5.."}[5m])

# Memoria heap usada por la API
dotnet_gc_heap_size_bytes{job="academy-api"}

# Conexiones activas a PostgreSQL
pg_stat_activity_count{job="postgres"}

# Operaciones por segundo en MongoDB
rate(mongodb_op_counters_total{job="mongodb"}[1m])
```

### Verificar que los targets están UP

1. Ir a http://localhost:9090/targets
2. Todos los targets deben aparecer en estado **UP** (fondo verde)
3. Si alguno aparece **DOWN**, revisar los logs: `docker compose logs <servicio>`

### Recargar configuración sin reiniciar

```bash
curl -X POST http://localhost:9090/-/reload
```

---

## Acceso a Grafana

**URL:** http://localhost:3000  
**Usuario:** `admin`  
**Contraseña:** `admin123`

### Dashboard precargado

Al iniciar sesión verás directamente el dashboard **Academy Manager — API Overview** con los siguientes paneles:

| Panel | Métrica |
|---|---|
| Peticiones HTTP por segundo | `rate(http_requests_received_total[1m])` |
| Latencia P50 / P95 / P99 | `histogram_quantile` sobre `http_request_duration_seconds` |
| Total de peticiones | Contador acumulado |
| Peticiones en curso | Gauge de concurrencia |
| Errores 4xx / 5xx | Tasa de errores con umbral visual |
| Health Check PostgreSQL | Estado del health check como métrica |
| Uso de memoria .NET (heap) | GC heap + working set del proceso |
| GC Collections por generación | Rate de Gen 0, 1, 2 |
| Top 10 rutas por volumen | Bar chart de rutas más llamadas |

### Añadir dashboards de la comunidad

Grafana tiene cientos de dashboards listos para importar:

1. Ir a **Dashboards → Import**
2. Introducir el ID del dashboard y pulsar **Load**

| Dashboard | ID | Para qué |
|---|---|---|
| ASP.NET Core (`prometheus-net`) | **10427** | Métricas detalladas del runtime .NET |
| PostgreSQL | **9628** | Conexiones, queries lentas, vacuums |
| MongoDB | **2583** | Operaciones, replicaset, memoria |
| Node Exporter (host) | **1860** | CPU, memoria y disco del host |

### Generar tráfico para ver las métricas

```bash
# Generar peticiones a la API para poblar las gráficas
for i in {1..50}; do
  curl -s http://localhost:5000/api/students > /dev/null
  curl -s http://localhost:5000/api/subjects > /dev/null
  sleep 0.2
done
```

---

## Métricas disponibles

### Métricas HTTP (prometheus-net)

Disponibles en `http://localhost:5000/metrics`:

| Métrica | Tipo | Descripción |
|---|---|---|
| `http_requests_received_total` | Counter | Peticiones totales por método, ruta y código HTTP |
| `http_request_duration_seconds` | Histogram | Distribución de latencia por ruta |
| `http_requests_in_progress` | Gauge | Peticiones HTTP activas en este momento |

### Métricas de runtime .NET

| Métrica | Descripción |
|---|---|
| `dotnet_gc_heap_size_bytes` | Tamaño del heap por generación GC |
| `dotnet_gc_collections_total` | Número de colecciones GC por generación |
| `dotnet_threadpool_threads_total` | Hilos activos en el thread pool |
| `process_cpu_seconds_total` | CPU consumida por el proceso |
| `process_working_set_bytes` | Memoria RAM usada por el proceso |

### Health check como métrica

| Métrica | Descripción |
|---|---|
| `healthcheck_status{name="postgres-write"}` | 1 = Healthy, 0 = Unhealthy |

---

## Estructura del proyecto

```
academy-manager/
├── src/
│   ├── AcademyManager.API/
│   │   ├── Program.cs              # UseHttpMetrics() + MapMetrics("/metrics")
│   │   ├── AcademyManager.API.csproj  # prometheus-net.AspNetCore añadido
│   │   └── ...
│   ├── AcademyManager.Application/
│   ├── AcademyManager.Infrastructure/
│   └── AcademyManager.Domain/
├── monitoring/
│   ├── prometheus.yml              # Configuración de scraping
│   └── grafana/
│       ├── provisioning/
│       │   ├── datasources/
│       │   │   └── prometheus.yml  # Datasource Prometheus (auto-provisionado)
│       │   └── dashboards/
│       │       └── dashboards.yml  # Configuración de carga de dashboards
│       └── dashboards/
│           └── academy-api.json    # Dashboard precargado con 9 paneles
├── scripts/
│   ├── postgres-init.sql
│   └── mongo-init.js
├── docker-compose.yml              # Incluye Prometheus, Grafana y exporters
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

### Paso 2 — Construir la imagen

```bash
eval $(minikube docker-env)
docker build -t academy-manager-api:latest .
```

### Paso 3 — Aplicar manifiestos de la aplicación

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secrets/
kubectl apply -f k8s/configmaps/
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/mongodb/
kubectl apply -f k8s/api/
```

### Paso 4 — Instalar Prometheus y Grafana con Helm

```bash
# Añadir repositorio de Helm
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

# Instalar kube-prometheus-stack (Prometheus + Grafana + AlertManager)
helm install monitoring prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --set grafana.adminPassword=admin123 \
  --set prometheus.prometheusSpec.scrapeInterval=15s
```

### Paso 5 — Añadir scrape config para la API

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

### Paso 6 — Acceder a Grafana en K8s

```bash
# Port-forward de Grafana
kubectl port-forward svc/monitoring-grafana 3000:80 -n monitoring

# En otro terminal — obtener la contraseña si no se configuró con --set
kubectl get secret monitoring-grafana -n monitoring \
  -o jsonpath="{.data.admin-password}" | base64 -d
```

Acceder en http://localhost:3000 con `admin` / `admin123`

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

