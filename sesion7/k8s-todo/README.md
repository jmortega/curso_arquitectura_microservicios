# Todo API — Hello World en Kubernetes

API mínima en .NET 8 para gestión de Todos en memoria.  
El objetivo es ilustrar el despliegue completo en un clúster **Kubernetes local con Minikube**.

---

## Estructura del proyecto

```
k8s-todo/
├── src/
│   ├── Program.cs          # Minimal API: endpoints CRUD + Swagger
│   └── TodoApi.csproj
├── k8s/
│   ├── namespace.yaml      # Namespace "todo-app"
│   ├── deployment.yaml     # 2 réplicas de la API
│   ├── service.yaml        # ClusterIP interno
│   └── ingress.yaml        # Acceso externo vía nginx
├── Dockerfile              # Build multi-stage
└── README.md
```

---

## Requisitos previos

| Herramienta | Versión mínima | Instalación |
|---|---|---|
| Docker | 24+ | https://docs.docker.com/get-docker |
| Minikube | 1.32+ | https://minikube.sigs.k8s.io/docs/start |
| kubectl | 1.28+ | https://kubernetes.io/docs/tasks/tools |

> En macOS puedes instalar todo con Homebrew:
> ```bash
> brew install minikube kubectl
> ```

---

## Despliegue paso a paso

### 1. Iniciar Minikube

```bash
minikube start --driver=docker --cpus=2 --memory=2048

# Verificar que el clúster está operativo
minikube status
kubectl cluster-info
```

Salida esperada de `minikube status`:
```
minikube
type: Control Plane
host: Running
kubelet: Running
apiserver: Running
kubeconfig: Configured
```

---

### 2. Habilitar el Ingress Controller

```bash
minikube addons enable ingress

# Esperar a que el pod de nginx esté listo (~30 seg)
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s
```
### 2.1 Obtener pods para un namespace concreto

```bash
$ kubectl get pods --namespace ingress-nginx
NAME                                        READY   STATUS      RESTARTS   AGE
ingress-nginx-admission-create-bxjt5        0/1     Completed   0          4m23s
ingress-nginx-admission-patch-wqc4j         0/1     Completed   1          4m23s
ingress-nginx-controller-596f8778bc-zdzxb   1/1     Running     0          4m23s
```

---

### 3. Construir la imagen Docker dentro de Minikube

Minikube tiene su propio daemon Docker. Al apuntar el CLI a él, la imagen queda disponible directamente para el clúster sin necesidad de un registry externo.

```bash
# Apuntar Docker CLI al daemon interno de Minikube
eval $(minikube docker-env)

# Construir la imagen
docker build -t todo-api:latest .

# Verificar que la imagen está disponible
docker images | grep todo-api
```

Salida esperada:
```
todo-api   latest   abc123def456   10 seconds ago   120MB
```

> Si cierras la terminal tendrás que ejecutar `eval $(minikube docker-env)` de nuevo.

---

### 4. Aplicar los manifiestos de Kubernetes, primero el namespace.yanl

```bash
# Aplicar todos los ficheros de k8s/ de una vez
kubectl apply -f k8s/

# O uno a uno si prefieres ver cada paso:
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml
```

```bash
kubectl apply -f k8s/namespace.yaml
kubectl wait --for=jsonpath='{.status.phase}'=Active namespace/todo-app --timeout=10s
kubectl apply -f k8s/
```

---

### 5. Verificar que todo está Running

```bash
# Ver todos los recursos del namespace
kubectl get all -n todo-app
```

Salida esperada:
```
NAME                            READY   STATUS    RESTARTS   AGE
pod/todo-api-6d9f8b7c4-abc12    1/1     Running   0          30s
pod/todo-api-6d9f8b7c4-xyz99    1/1     Running   0          30s

NAME                   TYPE        CLUSTER-IP      PORT(S)   AGE
service/todo-api-svc   ClusterIP   10.96.145.201   80/TCP    30s

NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
deployment.apps/todo-api   2/2     2            2           30s

NAME                                 DESIRED   CURRENT   READY   AGE
replicaset.apps/todo-api-d85b6b87d   2         2         1       21s

```

```bash
# Ver el Ingress
kubectl get ingress -n todo-app
```

Salida esperada:
```
NAME           CLASS   HOSTS        ADDRESS        PORTS   AGE
todo-ingress   nginx   todo.local   192.168.49.2   80      30s
```

$ kubectl describe ingress todo-ingress -n todo-app
Warning: v1 Endpoints is deprecated in v1.33+; use discovery.k8s.io/v1 EndpointSlice
Name:             todo-ingress
Labels:           <none>
Namespace:        todo-app
Address:          192.168.49.2
Ingress Class:    nginx
Default backend:  <default>
Rules:
  Host        Path  Backends
  ----        ----  --------
  *           
              /   todo-api-svc:80 (10.244.0.13:8080,10.244.0.14:8080)
Annotations:  nginx.ingress.kubernetes.io/rewrite-target: /
Events:
  Type    Reason  Age                    From                      Message
  ----    ------  ----                   ----                      -------
  Normal  Sync    5m22s (x2 over 5m37s)  nginx-ingress-controller  Scheduled for sync

---

### 6. Configurar el acceso local

Con minikube tunnel el Ingress se mapea a 127.0.0.1 (localhost) en lugar de la IP 192.168.49.2. 

$ sudo -E minikube tunnel
✅  Tunnel successfully started

📌  NOTE: Please do not close this terminal as this process must stay alive for the tunnel to be accessible ...

❗  The service/ingress todo-ingress requires privileged ports to be exposed: [80 443]
🔑  sudo permission will be asked for it.
🔗  Starting tunnel for service todo-ingress.


```bash
# Obtener la IP de Minikube
MINIKUBE_IP=$(minikube ip)
echo "IP de Minikube: $MINIKUBE_IP"

# Añadir la entrada en /etc/hosts (requiere sudo)
echo "$MINIKUBE_IP  todo.local" | sudo tee -a /etc/hosts
```

echo "127.0.0.1  todo.local" | sudo tee -a /etc/hosts

# Y acceder por nombre
curl http://todo.local/health
curl http://todo.local/todos

---

### 7. Acceder a la aplicación

Abre el navegador en **http://todo.local** — verás el Swagger UI directamente.

```bash
# Health check
curl http://todo.local/health

# Listar todos los Todos (3 precargados)
curl http://todo.local/todos

# Crear un nuevo Todo
curl -s -X POST http://todo.local/todos \
  -H "Content-Type: application/json" \
  -d '{"title": "Mi primer Todo en K8s"}' | jq .

# Marcar como completado (id: 1)
curl -s -X PUT http://todo.local/todos/1 \
  -H "Content-Type: application/json" \
  -d '{"title": "Aprender Kubernetes", "done": true}' | jq .

# Eliminar un Todo (id: 2)
curl -s -X DELETE http://todo.local/todos/2
```

---

## Comandos kubectl útiles

```bash
# Ver logs de un pod concreto
kubectl logs -n todo-app -l app=todo-api

# Seguir los logs en tiempo real
kubectl logs -n todo-app -l app=todo-api -f

# Ver detalles de un pod (útil para depurar errores de scheduling)
kubectl describe pod -n todo-app -l app=todo-api

# Abrir una shell dentro de un pod
kubectl exec -it -n todo-app \
  $(kubectl get pod -n todo-app -l app=todo-api -o jsonpath='{.items[0].metadata.name}') \
  -- /bin/sh

# Ver eventos del namespace (errores, scheduling, etc.)
kubectl get events -n todo-app --sort-by='.lastTimestamp'
```

---

## Escalar la aplicación

```bash
# Escalar a 4 réplicas
kubectl scale deployment todo-api --replicas=4 -n todo-app

# Volver a 2
kubectl scale deployment todo-api --replicas=2 -n todo-app

# Ver el estado del rollout
kubectl rollout status deployment/todo-api -n todo-app
```

---

## Actualizar la imagen (rolling update)

```bash
# 1. Reconstruir la imagen con una nueva etiqueta
eval $(minikube docker-env)
docker build -t todo-api:v2 .

# 2. Actualizar la imagen del deployment
kubectl set image deployment/todo-api todo-api=todo-api:v2 -n todo-app

# 3. Seguir el rollout
kubectl rollout status deployment/todo-api -n todo-app

# 4. Revertir si algo va mal
kubectl rollout undo deployment/todo-api -n todo-app
```

---

## Port-forward (alternativa al Ingress)

Si no quieres usar el Ingress puedes hacer port-forward directamente:

```bash
# Mapear el puerto 8080 local al servicio
kubectl port-forward svc/todo-api-svc 8080:80 -n todo-app

# Acceder en:  http://localhost:8080
```

---

## Limpiar el entorno

```bash
# Eliminar todos los recursos de la aplicación
kubectl delete namespace todo-app

# Parar Minikube (conserva el estado)
minikube stop

# Eliminar el clúster completamente
minikube delete
```

---

## Referencia de la API

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/todos` | Listar todos los Todos |
| `GET` | `/todos/{id}` | Obtener un Todo por ID |
| `POST` | `/todos` | Crear un nuevo Todo |
| `PUT` | `/todos/{id}` | Actualizar título y estado |
| `DELETE` | `/todos/{id}` | Eliminar un Todo |
| `GET` | `/health` | Health check (usado por K8s probes) |

La documentación interactiva Swagger está disponible en `http://todo.local/` (raíz).

> **Nota:** los datos son en memoria. Al reiniciar los pods los Todos vuelven a los 3 iniciales.
