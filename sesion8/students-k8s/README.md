# Students Service — Despliegue en Kubernetes con HPA

API REST de gestión de alumnos con autenticación JWT, MySQL y RabbitMQ.  
Incluye **HorizontalPodAutoscaler** para escalado automático y guía de simulación de problemas de escalado.

---

## Arquitectura

```
                    Internet / localhost
                           │
                    ┌──────▼───────┐
                    │    Ingress   │  students.local:80
                    │    nginx     │
                    └──────┬───────┘
                           │
              ┌────────────────────────────────────────┐
              │         Namespace: students             │
              │                                        │
              │  ┌─────────────────────────────────┐   │
              │  │       students-api (HPA)         │   │
              │  │   2-8 réplicas  puerto 8080      │   │
              │  │   ┌──────┐ ┌──────┐ ┌──────┐    │   │
              │  │   │pod-1 │ │pod-2 │ │pod-N │    │   │
              │  │   └──────┘ └──────┘ └──────┘    │   │
              │  └────────┬──────────────┬──────────┘   │
              │           │              │               │
              │    ┌──────▼──────┐ ┌────▼────────┐      │
              │    │    mysql    │ │  rabbitmq   │      │
              │    │  1 pod      │ │  1 pod      │      │
              │    │  :3306      │ │  :5672      │      │
              │    │  mysql-pvc  │ │  :15672(UI) │      │
              │    └─────────────┘ └─────────────┘      │
              └────────────────────────────────────────-─┘
```

---

## Estructura del proyecto

```
students-service-auth/
├── API/Controllers/
│   ├── AuthController.cs        # POST /register, POST /login
│   ├── StudentsController.cs    # CRUD /api/v1/students (JWT protegido)
│   └── InternalController.cs
├── Application/
│   ├── DTOs/                    # AuthDtos, StudentDtos
│   └── UseCases/                # AuthService, StudentService
├── Domain/
│   ├── Entities/                # Student, User
│   └── Interfaces/              # IStudentRepository, IUserRepository
├── Infrastructure/
│   ├── Configuration/           # ServiceExtensions (DI, JWT, Swagger)
│   ├── Messaging/               # RabbitMqStudentEventPublisher
│   └── Persistence/
│       ├── Migrations/init.sql  # DDL + seed data
│       └── Repositories/        # Dapper repositories
├── k8s/
│   ├── namespace.yaml
│   ├── config/
│   │   ├── secrets.yaml         # Credenciales MySQL, RabbitMQ, JWT (base64)
│   │   └── configmap.yaml       # Connection strings, configuración
│   ├── mysql/
│   │   └── mysql.yaml           # PVC + Deployment + Service
│   ├── rabbitmq/
│   │   └── rabbitmq.yaml        # Deployment + Service
│   └── api/
│       └── deployment.yaml      # Deployment + Service + Ingress + HPA ⬅
├── Dockerfile
├── docker-compose.yml
└── README.md
```

$ docker logs -f students-api 2>&1 | grep -i "RABBITMQ\|EVENT\|OBSERVER"
info: Students.Infrastructure.Messaging.RabbitMqStudentEventPublisher[0]
      [RABBITMQ] ✓ Publisher conectado al exchange 'students-events' en rabbitmq:5672
info: Students.Infrastructure.Messaging.RabbitMqStudentEventPublisher[0]
      [RABBITMQ] ✓ Publicado 'student.created' → exchange 'students-events'
info: Students.Infrastructure.Messaging.RabbitMqStudentEventPublisher[0]
      [RABBITMQ] ✓ Publicado 'student.created' → exchange 'students-events'

---

## Requisitos previos

| Herramienta | Versión mínima | Instalación |
|---|---|---|
| Docker | 24+ | https://docs.docker.com/get-docker |
| Minikube | 1.32+ | https://minikube.sigs.k8s.io/docs/start |
| kubectl | 1.28+ | https://kubernetes.io/docs/tasks/tools |

---

## Despliegue en Kubernetes con Minikube

### Paso 1 — Iniciar Minikube

```bash
minikube start --driver=docker --cpus=4 --memory=4096

minikube status
```

> Se recomiendan al menos 4 CPUs y 4 GB de RAM para que el HPA tenga margen de escalar.

### Paso 2 — Habilitar addons necesarios

```bash
# Ingress Controller (nginx)
minikube addons enable ingress

# Metrics Server — OBLIGATORIO para que el HPA funcione
minikube addons enable metrics-server

# Verificar
minikube addons list | grep -E "ingress|metrics-server"
```

### Paso 3 — Construir la imagen en Minikube

```bash
# Apuntar el CLI de Docker al daemon interno de Minikube
eval $(minikube docker-env)

# Construir la imagen
docker build -t students-api:latest .

# Verificar
docker images | grep students-api
```

### Paso 4 — Aplicar los manifiestos

```bash
# Namespace primero
kubectl apply -f k8s/namespace.yaml

# Configuración (secrets y configmap)
kubectl apply -f k8s/config/

# Bases de datos
kubectl apply -f k8s/mysql/
kubectl apply -f k8s/rabbitmq/

# Esperar a que MySQL y RabbitMQ estén listos
kubectl wait --for=condition=ready pod -l app=mysql     -n students --timeout=120s
kubectl wait --for=condition=ready pod -l app=rabbitmq  -n students --timeout=120s

# API + HPA
kubectl apply -f k8s/api/
```

### Paso 5 — Verificar el despliegue

```bash
kubectl get all -n students
kubectl get hpa  -n students
kubectl get ingress -n students
```

Salida esperada:
```
NAME                             READY   STATUS    RESTARTS   AGE
pod/mysql-xxx                    1/1     Running   0          2m
pod/rabbitmq-xxx                 1/1     Running   0          2m
pod/students-api-xxx-aaa         1/1     Running   0          90s
pod/students-api-xxx-bbb         1/1     Running   0          90s

NAME                       TYPE        CLUSTER-IP    PORT(S)   AGE
service/mysql-svc          ClusterIP   10.96.1.10    3306/TCP  2m
service/rabbitmq-svc       ClusterIP   10.96.1.11    5672/TCP  2m
service/students-api-svc   ClusterIP   10.96.1.12    80/TCP    90s

NAME                       READY   UP-TO-DATE   AVAILABLE
deployment/mysql           1/1     1            1
deployment/rabbitmq        1/1     1            1
deployment/students-api    2/2     2            2

NAME                                    REFERENCE             TARGETS           MINPODS   MAXPODS   REPLICAS
horizontalpodautoscaler/students-api    Deployment/students   10%/60%, 5%/70%   2         8         2
```

### Paso 6 — Acceder a la aplicación

```bash
# Obtener IP de Minikube
MINIKUBE_IP=$(minikube ip)

# Añadir entrada en /etc/hosts
echo "$MINIKUBE_IP  students.local" | sudo tee -a /etc/hosts

# Si el Ingress no responde, lanzar el tunnel en otro terminal
sudo -E minikube tunnel
```

**Swagger UI:** http://students.local/swagger

```bash
# Health check
curl http://students.local/health

# Registrar un usuario Admin
curl -s -X POST http://students.local/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","email":"admin@test.com","password":"Admin1234!","role":"Admin"}' | jq .

# Login — guardar el token
TOKEN=$(curl -s -X POST http://students.local/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin1234!"}' | jq -r '.token')

# Listar alumnos con el token
curl -s http://students.local/api/v1/students \
  -H "Authorization: Bearer $TOKEN" | jq .
```

---

## HorizontalPodAutoscaler — Cómo funciona

El HPA monitoriza las métricas de los pods cada **15 segundos** y calcula el número óptimo de réplicas:

```
réplicas deseadas = réplicas actuales × (métrica actual / umbral)

Ejemplo con CPU:
  2 réplicas × (90% / 60%) = 3 réplicas  → escala de 2 a 3
```

### Configuración en este proyecto

| Parámetro | Valor | Significado |
|---|---|---|
| `minReplicas` | 2 | Siempre hay al menos 2 pods (alta disponibilidad) |
| `maxReplicas` | 8 | Nunca escala más allá de 8 pods |
| `cpu averageUtilization` | 60% | Escala si la CPU media supera el 60% de `requests.cpu` (100m) |
| `memory averageUtilization` | 70% | Escala si la memoria media supera el 70% de `requests.memory` (128Mi) |
| `scaleUp stabilization` | 30s | Espera 30s antes de escalar hacia arriba |
| `scaleDown stabilization` | 120s | Espera 2 min antes de reducir réplicas |

### Ver el HPA en tiempo real

```bash
# Estado actual del HPA
kubectl get hpa -n students -w

# Descripción detallada con métricas actuales
kubectl describe hpa students-api-hpa -n students
```

---

## Simulación de problemas de escalado

### Escenario 1 — Pico de CPU (escalado correcto)

**Objetivo:** ver cómo el HPA crea nuevas réplicas cuando la CPU supera el umbral.

```bash
# Terminal 1: observar el HPA en tiempo real
kubectl get hpa -n students -w

# Terminal 2: generar carga CPU en los pods
kubectl run load-test \
  --image=busybox:1.36 \
  --restart=Never \
  -n students \
  -- sh -c "while true; do wget -q -O- http://students-api-svc/api/v1/students; done"
```

**Resultado esperado:** en 1-2 minutos el HPA escala de 2 a más réplicas.

**Para limpiar:**
```bash
kubectl delete pod load-test -n students
```

---

### Escenario 2 — Problema: Thrashing (escalado y desescalado continuo)

**Descripción del problema:** el HPA sube y baja réplicas continuamente porque la carga oscila alrededor del umbral. Esto causa inestabilidad en el servicio.

```
réplicas: 2 → 4 → 2 → 4 → 2 → ...  ← thrashing
```

**Cómo reproducirlo:**

```bash
# Generar carga intermitente que sube y baja
for i in {1..10}; do
  kubectl run load-$i \
    --image=busybox:1.36 --restart=Never -n students \
    -- sh -c "for j in {1..100}; do wget -q -O- http://students-api-svc/health; done"
  sleep 5
  kubectl delete pod load-$i -n students 2>/dev/null
done
```

**Síntoma:**
```bash
kubectl get hpa -n students -w
# REPLICAS cambia constantemente: 2, 4, 2, 4 ...
```

**Solución — aumentar las ventanas de estabilización:**

```yaml
behavior:
  scaleUp:
    stabilizationWindowSeconds: 60    # ← subir de 30s a 60s
  scaleDown:
    stabilizationWindowSeconds: 300   # ← subir de 120s a 300s (5 min)
    policies:
      - type: Pods
        value: 1
        periodSeconds: 120            # ← baja 1 pod cada 2 min como máximo
```

```bash
kubectl apply -f k8s/api/deployment.yaml
kubectl describe hpa students-api-hpa -n students
```

---

### Escenario 3 — Problema: HPA no escala (métricas unknown)

**Descripción del problema:** el HPA muestra `<unknown>` en las métricas y no escala aunque la aplicación esté saturada.

```
NAME               TARGETS              MINPODS  MAXPODS  REPLICAS
students-api-hpa   <unknown>/60%, ...   2        8        2
```

**Causa más frecuente:** metrics-server no está instalado o los pods no tienen `resources.requests` definidos.

**Diagnóstico:**

```bash
# Verificar que metrics-server está corriendo
kubectl get pods -n kube-system | grep metrics-server

# Ver métricas actuales de los pods
kubectl top pods -n students

# Si top falla, metrics-server no está disponible
```

**Solución A — habilitar metrics-server en Minikube:**

```bash
minikube addons enable metrics-server

# Esperar ~60 segundos y verificar
kubectl top pods -n students
kubectl get hpa -n students
```

**Solución B — verificar que los pods tienen resources.requests definidos:**

Sin `requests` el HPA no puede calcular el porcentaje de uso. Verificar en el Deployment:

```yaml
resources:
  requests:
    cpu: "100m"      # ← OBLIGATORIO para que el HPA calcule el %
    memory: "128Mi"  # ← OBLIGATORIO para metric de memoria
  limits:
    cpu: "500m"
    memory: "256Mi"
```

```bash
# Confirmar que los pods tienen requests configurados
kubectl describe pod -n students -l app=students-api | grep -A 6 "Requests:"
```

---

### Escenario 4 — Problema: Pods en Pending por falta de recursos

**Descripción del problema:** el HPA decide crear nuevos pods pero el clúster no tiene recursos suficientes. Los pods se quedan en estado `Pending`.

```bash
kubectl get pods -n students
# NAME                    READY   STATUS    RESTARTS
# students-api-xxx-aaa    1/1     Running
# students-api-xxx-bbb    1/1     Running
# students-api-xxx-ccc    0/1     Pending   ← no hay nodo con recursos libres
```

**Diagnóstico:**

```bash
# Ver por qué el pod está en Pending
kubectl describe pod -n students -l app=students-api | grep -A 10 "Events:"

# Ver recursos disponibles en los nodos
kubectl describe nodes | grep -A 5 "Allocated resources"
```

**Mensaje típico:**
```
0/1 nodes are available: 1 Insufficient cpu
```

**Solución A — aumentar los recursos de Minikube:**

```bash
minikube stop
minikube start --driver=docker --cpus=4 --memory=6144
```

**Solución B — reducir los limits del Deployment para que quepan más pods:**

```yaml
resources:
  requests:
    cpu: "50m"      # ← reducir para permitir más pods por nodo
    memory: "64Mi"
  limits:
    cpu: "200m"
    memory: "128Mi"
```

**Solución C — reducir maxReplicas del HPA al número de pods que soporta el clúster:**

```yaml
spec:
  maxReplicas: 4   # ← ajustar según los recursos disponibles
```

---

### Escenario 5 — Problema: Escalado lento ante pico repentino

**Descripción del problema:** llega un pico de tráfico repentino pero el HPA tarda demasiado en crear réplicas porque `stabilizationWindowSeconds` es demasiado alto o `maxSurge` es 1.

**Solución — configurar escalado agresivo hacia arriba:**

```yaml
behavior:
  scaleUp:
    stabilizationWindowSeconds: 0    # ← escala inmediatamente sin esperar
    policies:
      - type: Percent                # ← permite doblar las réplicas en un ciclo
        value: 100
        periodSeconds: 30
      - type: Pods
        value: 4                     # ← o bien: sube hasta 4 pods por ciclo
        periodSeconds: 30
    selectPolicy: Max                # ← usa la política que permita escalar más
  scaleDown:
    stabilizationWindowSeconds: 300  # ← conservador al bajar
```

```bash
kubectl apply -f k8s/api/deployment.yaml

# Simular pico repentino y ver que escala rápido
kubectl run spike --image=busybox:1.36 --restart=Never -n students \
  -- sh -c "for i in {1..1000}; do wget -q -O- http://students-api-svc/health; done"

kubectl get hpa -n students -w
```

---

## Comandos de referencia rápida

```bash
# Ver estado del HPA
kubectl get hpa -n students
kubectl describe hpa students-api-hpa -n students

# Ver métricas de los pods en tiempo real
kubectl top pods -n students

# Ver logs de la API
kubectl logs -n students -l app=students-api -f

# Escalar manualmente (anula el HPA temporalmente)
kubectl scale deployment students-api --replicas=4 -n students

# Ver eventos del namespace
kubectl get events -n students --sort-by='.lastTimestamp'

# Shell dentro de un pod
kubectl exec -it -n students \
  $(kubectl get pod -n students -l app=students-api \
    -o jsonpath='{.items[0].metadata.name}') -- /bin/sh

# Port-forward de emergencia
kubectl port-forward svc/students-api-svc 5001:80 -n students
# → http://localhost:5001/swagger
```

---

## Limpiar el entorno

```bash
kubectl delete namespace students

minikube stop
minikube delete
```

---

## Referencia de la API

| Método | Ruta | Rol requerido | Descripción |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | — | Crear usuario |
| `POST` | `/api/v1/auth/login` | — | Obtener token JWT |
| `GET` | `/api/v1/students` | Admin, Teacher, ReadOnly | Listar alumnos |
| `GET` | `/api/v1/students/{id}` | Admin, Teacher, ReadOnly | Obtener alumno |
| `POST` | `/api/v1/students` | Admin, Teacher | Crear alumno |
| `PUT` | `/api/v1/students/{id}` | Admin, Teacher | Actualizar alumno |
| `DELETE` | `/api/v1/students/{id}` | Admin | Eliminar alumno |
| `GET` | `/health` | — | Health check |
