# 📊 Academy Manager — Consultas PromQL por Exporter

Referencia de métricas disponibles para cada exporter del stack de observabilidad.
Todas las consultas pueden ejecutarse directamente en **Prometheus** (`http://localhost:9090`)
o usarse como fuente en paneles de **Grafana** (`http://localhost:3000`).

---

## Índice

1. [Node Exporter — Infraestructura del host](#1-node-exporter--infraestructura-del-host-9100)
2. [PostgreSQL Exporter — Base de datos de escritura](#2-postgresql-exporter--base-de-datos-de-escritura-9187)
3. [MongoDB Exporter — Base de datos de lectura](#3-mongodb-exporter--base-de-datos-de-lectura-9216)
4. [Academy API — Métricas de la aplicación .NET 8](#4-academy-api--métricas-de-la-aplicación-net-8-5000metrics)
5. [Prometheus — Auto-monitorización](#5-prometheus--auto-monitorización-9090)
6. [Dashboards recomendados en Grafana](#6-dashboards-recomendados-en-grafana)

---

## 1. Node Exporter — Infraestructura del host (`9100`)

> Mide el estado físico/virtual de la máquina: CPU, RAM, disco y red.
> Verificar que el endpoint responde: `http://localhost:9100/metrics`

### 🖥️ CPU

```promql
# Porcentaje de uso de CPU (todos los cores, últimos 5 min)
100 - (avg by (instance)(rate(node_cpu_seconds_total{mode="idle"}[5m])) * 100)

# Porcentaje de CPU en modo usuario (procesos de aplicación)
avg by (instance)(rate(node_cpu_seconds_total{mode="user"}[5m])) * 100

# Porcentaje de CPU en modo sistema (llamadas al kernel)
avg by (instance)(rate(node_cpu_seconds_total{mode="system"}[5m])) * 100

# Porcentaje de CPU en espera de I/O (iowait — indica cuellos de botella en disco)
avg by (instance)(rate(node_cpu_seconds_total{mode="iowait"}[5m])) * 100

# Carga media del sistema — últimos 1, 5 y 15 minutos
node_load1
node_load5
node_load15

# Número de CPUs disponibles en el host
count(node_cpu_seconds_total{mode="idle"})
```

### 🧠 Memoria RAM

```promql
# RAM disponible en bytes
node_memory_MemAvailable_bytes

# RAM disponible en MB (más legible)
node_memory_MemAvailable_bytes / 1024 / 1024

# RAM total del host en GB
node_memory_MemTotal_bytes / 1024 / 1024 / 1024

# Porcentaje de RAM usada
100 - ((node_memory_MemAvailable_bytes * 100) / node_memory_MemTotal_bytes)

# RAM en uso activo por procesos (excluye cache/buffers)
(node_memory_MemTotal_bytes - node_memory_MemAvailable_bytes) / 1024 / 1024

# Memoria swap usada (si hay swap configurada)
node_memory_SwapTotal_bytes - node_memory_SwapFree_bytes

# Porcentaje de swap usado
100 - ((node_memory_SwapFree_bytes * 100) / node_memory_SwapTotal_bytes)
```

### 💾 Disco

```promql
# Espacio disponible en la partición raíz (bytes)
node_filesystem_avail_bytes{mountpoint="/"}

# Espacio disponible en la partición raíz (GB)
node_filesystem_avail_bytes{mountpoint="/"} / 1024 / 1024 / 1024

# Porcentaje de disco usado en /
100 - ((node_filesystem_avail_bytes{mountpoint="/"} * 100)
        / node_filesystem_size_bytes{mountpoint="/"})

# Espacio total del disco en GB
node_filesystem_size_bytes{mountpoint="/"} / 1024 / 1024 / 1024

# Tasa de lectura de disco (bytes/s, últimos 5 min)
rate(node_disk_read_bytes_total[5m])

# Tasa de escritura en disco (bytes/s, últimos 5 min)
rate(node_disk_written_bytes_total[5m])

# Tiempo de espera en operaciones de disco (saturation)
rate(node_disk_io_time_seconds_total[5m])
```

### 🌐 Red

```promql
# Bytes recibidos por segundo en eth0
rate(node_network_receive_bytes_total{device="eth0"}[5m])

# Bytes transmitidos por segundo en eth0
rate(node_network_transmit_bytes_total{device="eth0"}[5m])

# Paquetes recibidos por segundo
rate(node_network_receive_packets_total{device="eth0"}[5m])

# Errores de red recibidos (indica problemas de conectividad)
rate(node_network_receive_errs_total{device="eth0"}[5m])

# Errores de red en transmisión
rate(node_network_transmit_errs_total{device="eth0"}[5m])

# Paquetes descartados en recepción
rate(node_network_receive_drop_total{device="eth0"}[5m])
```

### 🔢 Sistema

```promql
# Número de procesos en ejecución
node_procs_running

# Número total de procesos (incluye en espera)
node_procs_blocked

# Descriptores de fichero abiertos
node_filefd_allocated

# Tiempo que lleva el sistema encendido (segundos)
node_time_seconds - node_boot_time_seconds

# Conexiones TCP activas por estado
node_netstat_Tcp_CurrEstab

# Conexiones TIME_WAIT (puede indicar saturación de sockets)
node_sockstat_TCP_tw
```

---

## 2. PostgreSQL Exporter — Base de datos de escritura (`9187`)

> Mide el estado transaccional de PostgreSQL: conexiones, locks, commits y tamaño.
> Verificar que el endpoint responde: `http://localhost:9187/metrics`

### 🔌 Conexiones

```promql
# Total de conexiones activas en este momento
pg_stat_activity_count

# Conexiones activas por estado (active, idle, idle in transaction...)
pg_stat_activity_count{state="active"}
pg_stat_activity_count{state="idle"}
pg_stat_activity_count{state="idle in transaction"}

# Conexiones en estado "idle in transaction" — ALERTA si sube mucho
# Indica que el código .NET no está cerrando transacciones correctamente
pg_stat_activity_count{state="idle in transaction"}

# Número máximo de conexiones permitidas
pg_settings_max_connections

# Porcentaje de conexiones usadas respecto al máximo
(pg_stat_activity_count / pg_settings_max_connections) * 100
```

### 📝 Transacciones

```promql
# Tasa de commits por segundo (últimos 5 min)
rate(pg_stat_database_xact_commit{datname="academy_write"}[5m])

# Tasa de rollbacks por segundo — ALERTA si sube inesperadamente
rate(pg_stat_database_xact_rollback{datname="academy_write"}[5m])

# Ratio rollbacks / commits (debe mantenerse cercano a 0)
rate(pg_stat_database_xact_rollback{datname="academy_write"}[5m])
  / rate(pg_stat_database_xact_commit{datname="academy_write"}[5m])

# Deadlocks detectados por segundo (debe ser 0 en condiciones normales)
rate(pg_stat_database_deadlocks{datname="academy_write"}[5m])
```

### 🔒 Bloqueos (Locks)

```promql
# Número de locks activos en la base de datos de escritura
pg_locks_count{datname="academy_write"}

# Locks por modo (ExclusiveLock, ShareLock, RowExclusiveLock...)
pg_locks_count{datname="academy_write", mode="ExclusiveLock"}
pg_locks_count{datname="academy_write", mode="ShareLock"}

# Locks NO concedidos (en espera) — valores > 0 indican contención
pg_locks_count{datname="academy_write", granted="false"}
```

### 📦 Cache y I/O

```promql
# Tasa de bloques leídos desde disco (cache miss) — idealmente baja
rate(pg_stat_database_blks_read{datname="academy_write"}[5m])

# Tasa de bloques leídos desde shared_buffers (cache hit)
rate(pg_stat_database_blks_hit{datname="academy_write"}[5m])

# Ratio de cache hit (debe ser > 0.99 en producción)
rate(pg_stat_database_blks_hit{datname="academy_write"}[5m])
  / (rate(pg_stat_database_blks_hit{datname="academy_write"}[5m])
     + rate(pg_stat_database_blks_read{datname="academy_write"}[5m]))

# Filas insertadas por segundo en academy_write
rate(pg_stat_database_tup_inserted{datname="academy_write"}[5m])

# Filas actualizadas por segundo
rate(pg_stat_database_tup_updated{datname="academy_write"}[5m])

# Filas eliminadas por segundo
rate(pg_stat_database_tup_deleted{datname="academy_write"}[5m])

# Filas devueltas en consultas por segundo (carga de lectura en write DB)
rate(pg_stat_database_tup_returned{datname="academy_write"}[5m])
```

### 🗄️ Tamaño y Replicación

```promql
# Tamaño de la base de datos academy_write en MB
pg_database_size_bytes{datname="academy_write"} / 1024 / 1024

# Tamaño de todas las bases de datos
pg_database_size_bytes / 1024 / 1024

# Estado del servidor PostgreSQL (1 = up)
pg_up

# Tiempo desde el último autovacuum en segundos
time() - pg_stat_user_tables_last_autovacuum
```

---

## 3. MongoDB Exporter — Base de datos de lectura (`9216`)

> Mide el estado de MongoDB: operaciones, memoria, documentos y réplicas.
> Verificar que el endpoint responde: `http://localhost:9216/metrics`

### 🔌 Estado general

```promql
# MongoDB disponible (1 = up, 0 = down)
mongodb_up

# Uptime del servidor MongoDB en segundos
mongodb_instance_uptime_seconds

# Número de conexiones activas en este momento
mongodb_connections{state="current"}

# Conexiones disponibles restantes
mongodb_connections{state="available"}

# Porcentaje de conexiones usadas
(mongodb_connections{state="current"} * 100)
  / (mongodb_connections{state="current"} + mongodb_connections{state="available"})
```

### ⚡ Operaciones

```promql
# Tasa de operaciones por tipo por segundo (últimos 5 min)
rate(mongodb_op_counters_total{type="insert"}[5m])
rate(mongodb_op_counters_total{type="query"}[5m])
rate(mongodb_op_counters_total{type="update"}[5m])
rate(mongodb_op_counters_total{type="delete"}[5m])
rate(mongodb_op_counters_total{type="getmore"}[5m])
rate(mongodb_op_counters_total{type="command"}[5m])

# Total de operaciones por segundo (todas las colecciones)
sum(rate(mongodb_op_counters_total[5m]))

# Operaciones de lectura vs escritura (ratio)
rate(mongodb_op_counters_total{type="query"}[5m])
  / rate(mongodb_op_counters_total{type="insert"}[5m])
```

### 🧠 Memoria

```promql
# Memoria residente usada por MongoDB (MB)
mongodb_memory{type="resident"}

# Memoria virtual total asignada (MB)
mongodb_memory{type="virtual"}

# Memoria mapeada (solo en motor MMAPv1, legacy)
mongodb_memory{type="mapped"}

# Porcentaje de memoria residente respecto a la RAM total del host
(mongodb_memory{type="resident"} * 1024 * 1024 * 100)
  / node_memory_MemTotal_bytes
```

### 📄 Documentos y Colecciones

```promql
# Documentos insertados desde el inicio del servidor
mongodb_document_total{state="inserted"}

# Documentos actualizados desde el inicio
mongodb_document_total{state="updated"}

# Documentos eliminados desde el inicio
mongodb_document_total{state="deleted"}

# Tasa de documentos insertados por segundo (últimos 5 min)
rate(mongodb_document_total{state="inserted"}[5m])

# Tasa de documentos actualizados por segundo
rate(mongodb_document_total{state="updated"}[5m])
```

### 🔄 WiredTiger — Motor de almacenamiento

```promql
# Bytes escritos en el cache de WiredTiger
mongodb_mongod_wiredtiger_cache_bytes{type="written_from_cache"}

# Bytes leídos en el cache de WiredTiger
mongodb_mongod_wiredtiger_cache_bytes{type="read_into_cache"}

# Páginas leídas desde disco (cache miss en WiredTiger)
rate(mongodb_mongod_wiredtiger_cache_pages_total{type="read"}[5m])

# Tamaño actual del cache de WiredTiger (bytes)
mongodb_mongod_wiredtiger_cache_bytes{type="currently_in_cache"}

# Porcentaje de uso del cache WiredTiger
(mongodb_mongod_wiredtiger_cache_bytes{type="currently_in_cache"} * 100)
  / mongodb_mongod_wiredtiger_cache_bytes{type="maximum_bytes_configured"}
```

### 🔁 Replicación (si hay replica set)

```promql
# Estado del miembro en el replica set (1=PRIMARY, 2=SECONDARY, 6=UNKNOWN)
mongodb_replset_member_state

# Retraso de replicación del secondary respecto al primary (segundos)
max(mongodb_replset_member_optime_date{state="PRIMARY"})
  - max(mongodb_replset_member_optime_date{state="SECONDARY"})

# Latencia de operaciones en el oplog
mongodb_replset_oplog_tail_timestamp - mongodb_replset_oplog_head_timestamp
```

---

## 4. Academy API — Métricas de la aplicación .NET 8 (`5000/metrics`)

> Métricas generadas por `prometheus-net.AspNetCore` sobre la aplicación .NET 8.
> Verificar que el endpoint responde: `http://localhost:5000/metrics`

### 🌐 Peticiones HTTP

```promql
# Tasa de peticiones recibidas por segundo (últimos 5 min)
rate(http_requests_received_total[5m])

# Total de peticiones recibidas desde el inicio
http_requests_received_total

# Tasa de peticiones por código de respuesta HTTP
rate(http_requests_received_total{code="200"}[5m])
rate(http_requests_received_total{code="400"}[5m])
rate(http_requests_received_total{code="404"}[5m])
rate(http_requests_received_total{code="500"}[5m])

# Tasa de errores 5xx por segundo (ALERTA crítica)
rate(http_requests_received_total{code=~"5.."}[5m])

# Tasa de errores 4xx por segundo
rate(http_requests_received_total{code=~"4.."}[5m])

# Ratio de errores sobre el total de peticiones
rate(http_requests_received_total{code=~"5.."}[5m])
  / rate(http_requests_received_total[5m])

# Peticiones en curso en este momento
http_requests_in_progress
```

### ⏱️ Latencia (duración de peticiones)

```promql
# Duración media de las peticiones HTTP (segundos)
rate(http_request_duration_seconds_sum[5m])
  / rate(http_request_duration_seconds_count[5m])

# Percentil 50 de latencia (mediana)
histogram_quantile(0.50, rate(http_request_duration_seconds_bucket[5m]))

# Percentil 95 de latencia — referencia para SLAs
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Percentil 99 de latencia — detecta outliers
histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m]))

# Latencia por endpoint y método HTTP
histogram_quantile(0.95,
  rate(http_request_duration_seconds_bucket{method="POST"}[5m]))
```

### ⚙️ Runtime de .NET 8

```promql
# Uso de CPU del proceso .NET (segundos de CPU consumidos por segundo)
rate(process_cpu_seconds_total[5m])

# Memoria total asignada al proceso .NET (bytes)
process_virtual_memory_bytes

# Memoria residente del proceso .NET (working set)
process_resident_memory_bytes

# Número de hilos activos en el proceso
process_num_threads

# Número de descriptores de fichero abiertos por el proceso
process_open_fds

# Tiempo de inicio del proceso .NET
process_start_time_seconds
```

### 🗑️ Garbage Collector

```promql
# Número total de colecciones GC por generación
dotnet_collection_count_total{generation="0"}
dotnet_collection_count_total{generation="1"}
dotnet_collection_count_total{generation="2"}

# Tasa de colecciones GC por generación (por segundo)
rate(dotnet_collection_count_total{generation="0"}[5m])
rate(dotnet_collection_count_total{generation="1"}[5m])
rate(dotnet_collection_count_total{generation="2"}[5m])

# Memoria total asignada por el GC (bytes acumulados)
dotnet_total_memory_bytes

# Tamaño del heap de .NET en bytes
process_working_set_bytes
```

### 🧵 ThreadPool

```promql
# Hilos disponibles en el ThreadPool (debe mantenerse alto)
dotnet_threadpool_num_threads

# Tamaño actual del ThreadPool
dotnet_threadpool_num_threads

# Cola de trabajo del ThreadPool (items pendientes)
dotnet_threadpool_queue_length

# Hilos completados por segundo
rate(dotnet_threadpool_completed_items_total[5m])
```

---

## 5. Prometheus — Auto-monitorización (`9090`)

> Métricas internas de Prometheus: estado de scrapes, series activas y rendimiento.

```promql
# Estado de cada target (1 = UP, 0 = DOWN)
up

# Últimas veces que cada job fue scrapeado con éxito
scrape_duration_seconds

# Número de series temporales activas en Prometheus
prometheus_tsdb_head_series

# Número total de muestras ingeridas por segundo
rate(prometheus_tsdb_head_samples_appended_total[5m])

# Errores de scraping por job
rate(scrape_samples_scraped[5m])

# Tiempo de duración del último scrape por job (segundos)
scrape_duration_seconds{job="node-metrics"}
scrape_duration_seconds{job="academy-api"}
scrape_duration_seconds{job="postgres-metrics"}
scrape_duration_seconds{job="mongodb-metrics"}

# Espacio en disco usado por Prometheus (bytes)
prometheus_tsdb_storage_blocks_bytes

# Número de chunks de datos en memoria
prometheus_tsdb_head_chunks

# Tamaño total del WAL (Write Ahead Log)
prometheus_tsdb_wal_storage_size_bytes

# Peticiones a la API de Prometheus por handler
rate(prometheus_http_requests_total[5m])

# Número de reglas de alerta activas
prometheus_rule_group_rules
```

---

## 6. Dashboards recomendados en Grafana

Importar desde `Grafana → Dashboards → Import → ID`:

| ID    | Nombre                          | Exporter            | Notas                                    |
|-------|---------------------------------|---------------------|------------------------------------------|
| 1860  | Node Exporter Full              | Node Exporter       | Dashboard oficial más completo           |
| 9628  | PostgreSQL Database             | postgres-exporter   | Conexiones, locks y transacciones        |
| 7362  | MongoDB Overview                | mongodb-exporter    | Ops, memoria y estado del replica set    |
| 10427 | .NET Core Prometheus Metrics    | Academy API         | Runtime, GC y ThreadPool de .NET         |
| 3662  | Prometheus 2.0 Overview         | Prometheus          | Estado interno del propio Prometheus     |

---

## 7. Consultas combinadas — Vista global del sistema

```promql
# Resumen de salud: todos los targets activos
count(up == 1)

# Targets caídos (valor > 0 dispara alerta)
count(up == 0)

# Top 3 servicios con mayor latencia P95
topk(3, histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])))

# Correlación: peticiones de escritura (.NET) vs commits en PostgreSQL
# — deben estar alineadas en el tiempo
rate(http_requests_received_total{code="200"}[5m])
rate(pg_stat_database_xact_commit{datname="academy_write"}[5m])

# Presión de memoria total del sistema (host + procesos .NET + PostgreSQL + MongoDB)
node_memory_MemTotal_bytes - node_memory_MemAvailable_bytes
```

---

> **Tip — Recarga de configuración sin reiniciar Prometheus:**
> ```bash
> curl -X POST http://localhost:9090/-/reload
> ```
> Funciona gracias al flag `--web.enable-lifecycle` definido en el `docker-compose.yml`.
