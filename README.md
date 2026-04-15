# Arquitectura de Microservicios, DDD, Hexagonal, EDA y CQRS en .NET

Guía de preparación y referencia para el curso. Aquí encontrarás los requisitos previos, herramientas necesarias y la configuración de cada sesión.

---

## Requisitos previos

### Conocimientos
- Experiencia desarrollando en **C# 12/13 o superior en .NET 8 o superior****

### Hardware
- Mínimo **16 GB de RAM**
- **150 GB** de espacio libre en disco
- Usuario con **permisos de instalación** de software
- En equipos Windows: **WSL 2** habilitado y operativo

### Software
Antes del inicio del curso debes tener instalado, actualizado y configurado:

| Herramienta | Versión recomendada | Enlace |
|---|---|---|
| SDK .NET | 8 LTS | https://dotnet.microsoft.com |
| Visual Studio | Community / Professional / Enterprise | https://visualstudio.microsoft.com |
| Docker Desktop | Última estable | https://www.docker.com |
| Git | Última estable | https://git-scm.com |
| Postman | Última estable | https://www.postman.com |
| minikube | Última estable | https://minikube.sigs.k8s.io |
| kubectl | 1.30+ | https://kubernetes.io/docs/tasks/tools |
| Helm | Última estable | https://helm.sh |
| Zoom | Última estable | https://zoom.us |

> En Visual Studio asegúrate de tener instalada la carga de trabajo **ASP.NET and web development**.

### Entorno de desarrollo recomendado para las primeras sesiones

1. Compilar una solución .NET 8 con Visual Studio o Visual Studio Code
2. Ejecutar una API en local y probarla con Postman o swagger
3. Levantar un contenedor Docker
4. Verificar conectividad con base de datos local(MySQL)

> Si trabajas en una empresa, coordina previamente con el equipo de sistemas para desbloquear acceso a paquetes NuGet, imágenes Docker y repositorios del curso.

---

## Sesiones

### Sesión 1 — Mensajería con RabbitMQ y MassTransit (21 Abril)

Introducción a la comunicación asíncrona entre microservicios usando **RabbitMQ** como broker de mensajes y **MassTransit** como librería de abstracción.

- RabbitMQ: https://www.rabbitmq.com
- MassTransit: https://masstransit-project.com

#### Levantar RabbitMQ con Docker

**Opción rápida:**
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

**Con docker-compose:**
```yaml
version: "3.9"

services:
  rabbitmq:
    image: rabbitmq:3.13-management
    container_name: rabbitmq-masstransit
    ports:
      - "5672:5672"     # Puerto AMQP (MassTransit se conecta aquí)
      - "15672:15672"   # UI de gestión web
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
      RABBITMQ_DEFAULT_VHOST: /
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  rabbitmq_data:
```

```bash
docker compose up -d
```

| Acceso | URL | Credenciales |
|---|---|---|
| UI de gestión | http://localhost:15672 | guest / guest |
| Puerto AMQP | localhost:5672 | — |

---

### Sesión 3 — GitHub y CI/CD (28 Abril)

En esta sesión se crearán **tokens de acceso en GitHub** para integración con pipelines de CI/CD.

> Asegúrate de tener cuenta activa en [github.com](https://github.com) antes de la sesión.

---

### Sesión 7 — Kubernetes con minikube (12 Mayo)

Creación de un clúster local de Kubernetes usando **minikube**.

#### Requisitos mínimos para minikube
- 2 CPUs o más
- 2 GB de RAM libres
- 20 GB de espacio libre en disco
- Conexión a internet
- Docker, VirtualBox, Hyper-V, KVM o cualquier otro gestor de contenedores/VMs compatible

#### Instalación en Linux
```bash
# minikube
curl -LO https://github.com/kubernetes/minikube/releases/latest/download/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube && rm minikube-linux-amd64

# kubectl (distribuciones basadas en Debian/Ubuntu)
sudo apt-get update
sudo apt-get install -y apt-transport-https ca-certificates curl

curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.30/deb/Release.key \
  | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg

echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.30/deb/ /' \
  | sudo tee /etc/apt/sources.list.d/kubernetes.list

sudo apt-get update
sudo apt-get install -y kubectl
```

#### Verificación
```bash
minikube start
kubectl get nodes
```

---

### Sesión 9 — Monitorización y Observabilidad (19 Mayo)

Introducción al stack de observabilidad: métricas, trazas distribuidas y visualización.

---

#### Prometheus + Grafana

Fichero `prometheus.yml`:
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  # Descomentar para añadir microservicios .NET:
  # - job_name: 'dotnet-api'
  #   static_configs:
  #     - targets: ['host.docker.internal:8080']
```

Fichero `docker-compose.yml`:
```yaml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus
    container_name: prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana
    container_name: grafana
    ports:
      - "3000:3000"
    depends_on:
      - prometheus
```

| Herramienta | URL |
|---|---|
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |

---

#### Zipkin

**Opción rápida:**
```bash
docker run -d -p 9411:9411 --name zipkin openzipkin/zipkin
```

**Con docker-compose:**
```yaml
version: '3.8'

services:
  zipkin:
    image: openzipkin/zipkin
    container_name: zipkin
    ports:
      - "9411:9411"
    environment:
      - STORAGE_TYPE=mem
```

| Herramienta | URL |
|---|---|
| Zipkin UI | http://localhost:9411 |

---

#### Jaeger + OpenTelemetry Collector

Fichero `otel-collector-config.yaml`:
```yaml
receivers:
  otlp:
    protocols:
      grpc:
      http:

exporters:
  logging:
    verbosity: normal
  jaeger:
    endpoint: "jaeger:14250"
    tls:
      insecure: true

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: []
      exporters: [logging, jaeger]
```

Fichero `docker-compose.yml`:
```yaml
version: '3.8'

services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: jaeger
    ports:
      - "16686:16686"   # Interfaz web
      - "14250:14250"   # Recibe trazas del Collector vía gRPC
    restart: always

  otel-collector:
    image: otel/opentelemetry-collector:latest
    container_name: otel-collector
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./otel-collector-config.yaml:/etc/otel-collector-config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC receiver
      - "4318:4318"   # OTLP HTTP receiver
    depends_on:
      - jaeger
```

```bash
docker compose up -d
```

| Herramienta | URL |
|---|---|
| Jaeger UI | http://localhost:16686 |
| OTLP gRPC | localhost:4317 |
| OTLP HTTP | localhost:4318 |

---

## Stack tecnológico del curso

| Componente | Tecnología |
|---|---|
| Lenguaje | C# / .NET 8 |
| Base de datos | MySQL 8.0 |
| Contenedores | Docker (multi-stage builds) |
| Orquestador | Kubernetes — minikube |
| API Gateway | YARP (Yet Another Reverse Proxy) |
| Mensajería | RabbitMQ + MassTransit |
| Monitorización | Prometheus + Grafana |
| Trazas distribuidas | Jaeger + OpenTelemetry / Zipkin |
| ORM | Dapper |
| Ingress | NGINX Ingress Controller |
