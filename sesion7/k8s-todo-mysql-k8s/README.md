# Todo API — .NET 8 + MySQL

API mínima en .NET 8 para gestión de Todos persistidos en **MySQL**.  
Soporta dos modos de despliegue: **Docker Compose** (desarrollo local) y **Kubernetes con Minikube** (entorno de clúster).

---

## Arquitectura

```
┌──────────────────────────────────────────────────────────────┐
│                      todo-app (namespace)                     │
│                                                               │
│   ┌──────────────────┐           ┌─────────────────────┐     │
│   │    todo-api      │           │        mysql         │     │
│   │  2 réplicas      │──────────▶│  1 pod               │     │
│   │  puerto 8080     │  mysql-svc│  puerto 3306         │     │
│   └──────────────────┘  ClusterIP└──────────┬────────────┘    │
│           │                                 │                 │
│    todo-api-svc                         mysql-pvc             │
│    ClusterIP:80                       (1Gi volumen)           │
│           │                                                   │
│    todo-ingress                                               │
│    nginx → todo.local                                         │
└───────────┼───────────────────────────────────────────────────┘
            │
     http://todo.local  (K8s)
     http://localhost:5000  (Docker Compose)
```

---

## Estructura del proyecto

```
k8s-todo-mysql/
├── src/
│   ├── Program.cs                           # Minimal API: CRUD + arranque con reintentos
│   ├── TodoApi.csproj                       # NuGet: EF Core + Pomelo MySQL + Swagger
│   ├── appsettings.json                     # Connection string → servicio "mysql"
│   ├── appsettings.Development.json         # Connection string → localhost
│   ├── Data/
│   │   └── TodoDbContext.cs                 # DbContext + entidad Todo + seed data
│   └── Migrations/
│       ├── 20240101000000_InitialCreate.cs  # Crea tabla todos + seed
│       └── TodoDbContextModelSnapshot.cs
├── k8s/
│   ├── namespace.yaml                       # Namespace "todo-app"
│   ├── config/
│   │   ├── secret.yaml                      # Credenciales MySQL (base64)
│   │   └── configmap.yaml                   # Connection string para la API
│   ├── mysql/
│   │   └── mysql.yaml                       # PVC + Deployment + Service (ClusterIP)
│   └── api/
│       └── api.yaml                         # Deployment + Service + Ingress
├── Dockerfile                               # Build multi-stage
├── docker-compose.yml                       # API + MySQL juntos
└── README.md
```

---

## Requisitos previos

### Docker Compose

| Herramienta | Versión mínima | Instalación |
|---|---|---|
| Docker + Compose v2 | 24+ | https://docs.docker.com/get-docker |

### Kubernetes / Minikube

| Herramienta | Versión mínima | Instalación |
|---|---|---|
| Docker | 24+ | https://docs.docker.com/get-docker |
| Minikube | 1.32+ | https://minikube.sigs.k8s.io/docs/start |
| kubectl | 1.28+ | https://kubernetes.io/docs/tasks/tools |

---

## Opción A — Despliegue con Docker Compose

### 1. Construir e iniciar

```bash
docker compose up --build
```

Docker Compose levanta MySQL, espera al healthcheck y luego levanta la API, que aplica las migraciones automáticamente. En segundo plano:

```bash
docker compose up --build -d
```

### 2. Acceder

| Recurso | URL |
|---|---|
| Swagger UI | http://localhost:5000 |
| Health check | http://localhost:5000/health |
| MySQL directo | localhost:3306 |

### 3. Comandos útiles

```bash
# Ver logs en tiempo real
docker compose logs -f

# Solo la API
docker compose logs -f todo-api

# Conectarse a MySQL
docker exec -it todo-mysql mysql -u todo_user -ptodo_pass tododb

# Parar (conserva datos en el volumen)
docker compose down

# Parar y borrar también los datos
docker compose down -v
```

---

## Opción B — Despliegue en Kubernetes con Minikube

### Paso 1 — Iniciar Minikube

```bash
minikube start --driver=docker --cpus=2 --memory=2048

# Verificar estado
minikube status
kubectl cluster-info
```

### Paso 2 — Habilitar el Ingress Controller

```bash
minikube addons enable ingress

# Esperar a que el pod nginx esté listo (~30 s)
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s
```

### Paso 3 — Construir la imagen dentro de Minikube

Minikube tiene su propio daemon Docker. Al apuntar el CLI a él, la imagen queda disponible para el clúster sin necesitar un registry externo.

```bash
# Apuntar Docker CLI al daemon interno de Minikube
eval $(minikube docker-env)

# Verificar que estamos en el contexto correcto
docker info | grep "Name"
# Debe mostrar: Name: minikube

# Construir la imagen
docker build -t todo-api:latest .

# Confirmar que la imagen está disponible en Minikube
docker images | grep todo-api
```

> Si cierras la terminal tendrás que ejecutar `eval $(minikube docker-env)` de nuevo en la nueva sesión.

### Paso 4 — Aplicar los manifiestos

```bash
# Aplicar en orden: configuración → MySQL → API
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/config/
kubectl apply -f k8s/mysql/
kubectl apply -f k8s/api/
```

O todo a la vez de forma recursiva:

```bash
kubectl apply -f k8s/ --recursive
```

### Paso 5 — Esperar a que los pods estén Running

```bash
# Ver el estado general
kubectl get all -n todo-app

# Esperar a que MySQL esté listo (tarda ~30-60 s la primera vez)
kubectl wait --for=condition=ready pod \
  -l app=mysql \
  -n todo-app \
  --timeout=120s

# Esperar a que la API esté lista
kubectl wait --for=condition=ready pod \
  -l app=todo-api \
  -n todo-app \
  --timeout=120s
```

Salida esperada de `kubectl get all -n todo-app`:

```
NAME                            READY   STATUS    RESTARTS   AGE
pod/mysql-69dbdc49b-z4s2x       1/1     Running   0          6m52s
pod/todo-api-54bf9d95ff-kcsvm   1/1     Running   0          6m47s
pod/todo-api-54bf9d95ff-njt8n   1/1     Running   0          6m47s

NAME                   TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)    AGE
service/mysql-svc      ClusterIP   10.109.211.210   <none>        3306/TCP   6m52s
service/todo-api-svc   ClusterIP   10.108.50.98     <none>        80/TCP     6m47s

NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
deployment.apps/mysql      1/1     1            1           6m52s
deployment.apps/todo-api   2/2     2            2           6m47s

NAME                                  DESIRED   CURRENT   READY   AGE
replicaset.apps/mysql-69dbdc49b       1         1         1       6m52s
replicaset.apps/todo-api-54bf9d95ff   2         2         2       6m47s
```

### Paso 5.1 — Configurar tunnel

```bash
$ sudo -E minikube tunnel
[sudo] contraseña para linux:      
✅  Tunnel successfully started

📌  NOTE: Please do not close this terminal as this process must stay alive for the tunnel to be accessible ...

❗  The service/ingress todo-ingress requires privileged ports to be exposed: [80 443]
🔑  sudo permission will be asked for it.
🔗  Starting tunnel for service todo-ingress.
```

### Paso 6 — Configurar el acceso local

```bash
# Obtener la IP de Minikube
MINIKUBE_IP=$(minikube ip)
echo "IP de Minikube: $MINIKUBE_IP"

# Añadir la entrada en /etc/hosts (requiere sudo)
echo "$MINIKUBE_IP  todo.local" | sudo tee -a /etc/hosts
```

### Paso 7 — Acceder a la aplicación

| Recurso | URL |
|---|---|
| Swagger UI | https://todo.local |
| Health check | https://todo.local/health |
| Todos | https://todo.local/todos |

```bash
# Verificar que todo funciona
curl -s https://todo.local/health | jq .

# Listar Todos (3 precargados por el seed)
curl -s https://todo.local/todos | jq .

# Crear un Todo
curl -s -X POST https://todo.local/todos \
  -H "Content-Type: application/json" \
  -d '{"title": "Mi primer Todo en K8s con MySQL"}' | jq .
```

---

## Flujo de arranque en K8s

El siguiente diagrama muestra el orden en que Kubernetes levanta los recursos y las dependencias entre ellos:

```
kubectl apply --recursive
     │
     ├─ 1. namespace.yaml   → crea "todo-app"
     ├─ 2. secret.yaml      → credenciales MySQL en base64
     ├─ 3. configmap.yaml   → connection string para la API
     ├─ 4. mysql.yaml
     │       ├─ PVC          → reserva 1Gi de almacenamiento
     │       ├─ Deployment   → arranca el pod MySQL
     │       │     └─ readinessProbe: mysqladmin ping cada 10s
     │       └─ Service      → ClusterIP "mysql-svc:3306"
     │
     │              [MySQL pasa a Ready]
     │
     └─ 5. api.yaml
             ├─ Deployment
             │     ├─ initContainer: nc -z mysql-svc 3306
             │     │       └─ [bloquea hasta conexión TCP OK]
             │     ├─ container todo-api
             │     │       └─ MigrateAsync() → tabla + seed data
             │     └─ readinessProbe: GET /health cada 10s
             ├─ Service      → ClusterIP "todo-api-svc:80"
             └─ Ingress      → todo.local → todo-api-svc:80
```

El **init container** (`busybox`) garantiza que el contenedor de la API no arranca hasta que MySQL acepta conexiones TCP. Adicionalmente, `Program.cs` reintenta `EnsureCreatedAsync()` hasta 10 veces con 3 segundos entre intentos.

---

## Comandos kubectl útiles

```bash
# Ver logs de la API (todos los pods)
kubectl logs -n todo-app -l app=todo-api

# Seguir logs en tiempo real
kubectl logs -n todo-app -l app=todo-api -f

# Ver logs del init container si la API no arranca
kubectl logs -n todo-app -l app=todo-api -c wait-for-mysql

# Ver logs de MySQL
kubectl logs -n todo-app -l app=mysql -f

# Describir un pod (eventos, estado de probes, errores)
kubectl describe pod -n todo-app -l app=todo-api

# Abrir shell en un pod de la API
kubectl exec -it -n todo-app \
  $(kubectl get pod -n todo-app -l app=todo-api \
    -o jsonpath='{.items[0].metadata.name}') \
  -- /bin/sh

# Conectarse a MySQL desde dentro del clúster
kubectl exec -it -n todo-app \
  $(kubectl get pod -n todo-app -l app=mysql \
    -o jsonpath='{.items[0].metadata.name}') \
  -- mysql -u todo_user -ptodo_pass tododb

# Ver eventos del namespace (útil para depurar errores de scheduling)
kubectl get events -n todo-app --sort-by='.lastTimestamp'
```

---

## Escalar y actualizar

```bash
# Escalar la API a 4 réplicas
kubectl scale deployment todo-api --replicas=4 -n todo-app

# Volver a 2
kubectl scale deployment todo-api --replicas=2 -n todo-app

# Actualizar con nueva imagen (rolling update zero-downtime)
eval $(minikube docker-env)
docker build -t todo-api:v2 .
kubectl set image deployment/todo-api todo-api=todo-api:v2 -n todo-app
kubectl rollout status deployment/todo-api -n todo-app

# Revertir si algo va mal
kubectl rollout undo deployment/todo-api -n todo-app
```

---

## Acceso de emergencia vía port-forward

Si el Ingress no está disponible puedes acceder directamente al Service:

```bash
kubectl port-forward svc/todo-api-svc 5000:80 -n todo-app
# → http://localhost:5000
```

---

## Limpiar el entorno

```bash
# Eliminar todos los recursos de la aplicación
kubectl delete namespace todo-app

# Parar Minikube (conserva el estado para la próxima vez)
minikube stop

# Eliminar el clúster completamente
minikube delete
```

---

## Referencia de la API

| Método | Ruta | Body | Descripción |
|---|---|---|---|
| `GET` | `/todos` | — | Listar todos los Todos |
| `GET` | `/todos/{id}` | — | Obtener un Todo por ID |
| `POST` | `/todos` | `{"title":"..."}` | Crear un nuevo Todo |
| `PUT` | `/todos/{id}` | `{"title":"...","done":true}` | Actualizar un Todo |
| `DELETE` | `/todos/{id}` | — | Eliminar un Todo |
| `GET` | `/health` | — | Estado de la API y conexión a MySQL |
