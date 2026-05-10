# Todo API — .NET 8 + MySQL + Gateway API

API mínima en .NET 8 para gestión de Todos persistidos en **MySQL**.  
El acceso externo al clúster se gestiona con **Gateway API** (sucesor del Ingress), implementado con **Envoy Gateway**.

---

## Gateway API vs Ingress

| | Ingress | Gateway API |
|---|---|---|
| Recurso de entrada | `Ingress` | `Gateway` + `HTTPRoute` |
| Separación de roles | No | Sí (infraestructura vs aplicación) |
| Multiprotocolo | Solo HTTP/HTTPS | HTTP, gRPC, TCP, UDP |
| Flexibilidad | Limitada, vía anotaciones | Alta, recursos tipados |
| Estado | Estable, legacy | Estable desde K8s 1.28 |

Con Gateway API la responsabilidad se divide en dos recursos distintos:

- **`Gateway`** — define el punto de entrada al clúster (puerto, protocolo). Lo gestiona el equipo de infraestructura.
- **`HTTPRoute`** — define las reglas de enrutamiento hacia los servicios. Lo gestiona el equipo de aplicación.

---

## Arquitectura

```
                    Internet / localhost
                           │
                    Puerto 80 (HTTP)
                           │
                    ┌──────▼───────┐
                    │   Gateway    │  todo-gateway
                    │ Envoy Proxy  │  namespace: todo-app
                    └──────┬───────┘
                           │  HTTPRoute: todo-route
                           │  path: / → todo-api-svc:80
                    ┌──────▼───────────────────────────┐
                    │         todo-app (namespace)       │
                    │                                    │
                    │  ┌─────────────┐  ┌───────────┐   │
                    │  │  todo-api   │  │   mysql   │   │
                    │  │  2 réplicas │─▶│  1 pod    │   │
                    │  │  :8080      │  │  :3306    │   │
                    │  └─────────────┘  └─────┬─────┘   │
                    │  todo-api-svc:80    mysql-pvc      │
                    │  ClusterIP          1Gi volumen    │
                    └──────────────────────────────────-─┘
```

---

## Estructura del proyecto

```
k8s-todo-gateway/
├── src/
│   ├── Program.cs
│   ├── TodoApi.csproj
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Data/
│   │   └── TodoDbContext.cs
│   └── Migrations/
├── k8s/
│   ├── namespace.yaml               # Namespace "todo-app"
│   ├── config/
│   │   ├── secret.yaml              # Credenciales MySQL
│   │   └── configmap.yaml           # Connection string
│   ├── mysql/
│   │   └── mysql.yaml               # PVC + Deployment + Service (ClusterIP)
│   └── api/
│       ├── deployment.yaml          # Deployment + Service (ClusterIP)
│       └── gateway.yaml             # Gateway + HTTPRoute  ← novedad
├── Dockerfile
├── docker-compose.yml
└── README.md
```

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
minikube start --driver=docker --cpus=2 --memory=2048

minikube status
```

### Paso 2 — Instalar Gateway API CRDs

Los CRDs (Custom Resource Definitions) definen los nuevos tipos de recursos `Gateway` y `HTTPRoute` en Kubernetes. Sin este paso el clúster no reconoce esos recursos.

```bash
kubectl apply -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.2.0/standard-install.yaml

# Verificar que los CRDs están instalados
kubectl get crd | grep gateway

Salida esperada:
```
gateways.gateway.networking.k8s.io
httproutes.gateway.networking.k8s.io
referencegrants.gateway.networking.k8s.io
```

$ kubectl get gatewayclass
NAME   CONTROLLER                                      ACCEPTED   AGE
eg     gateway.envoyproxy.io/gatewayclass-controller   True       27m

cat <<EOF | kubectl apply -f -
apiVersion: gateway.networking.k8s.io/v1
kind: GatewayClass
metadata:
  name: eg
spec:
  controllerName: gateway.envoyproxy.io/gatewayclass-controller
EOF

Una vez el GatewayClass esté ACCEPTED: True, el controller detectará tu Gateway automáticamente. Fuerza la reconciliación re-aplicando el Gateway:

kubectl delete gateway todo-gateway -n todo-app
kubectl apply -f k8s/api/gateway.yaml

# Seguir el estado
kubectl get gateway todo-gateway -n todo-app -w

```



### Paso 3 — Instalar Envoy Gateway (el controller)

Los CRDs solo definen la estructura. Envoy Gateway es el controller que lee esos recursos y gestiona el tráfico real.

```bash
kubectl apply \
  --server-side \
  --force-conflicts \
  -f https://github.com/envoyproxy/gateway/releases/download/v1.2.0/install.yaml

# Esperar a que Envoy Gateway esté listo
kubectl wait --for=condition=ready pod \
  --all \
  -n envoy-gateway-system \
  --timeout=120s

# Verificar
kubectl get pods -n envoy-gateway-system
```

Salida esperada:
```
NAME                              READY   STATUS    RESTARTS   AGE
envoy-gateway-xxx                 1/1     Running   0          30s
```

### Paso 4 — Construir la imagen en Minikube

```bash
# Apuntar Docker CLI al daemon de Minikube
eval $(minikube docker-env)

# Construir
docker build -t todo-api:latest .

# Verificar
docker images | grep todo-api
```

### Paso 5 — Desplegar la aplicación

```bash
# Namespace y configuración primero
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/config/
kubectl apply -f k8s/mysql/
kubectl apply -f k8s/api/
```

### Paso 6 — Esperar a que todo esté Running

```bash
# Esperar a MySQL
kubectl wait --for=condition=ready pod \
  -l app=mysql -n todo-app --timeout=120s

# Esperar a la API
kubectl wait --for=condition=ready pod \
  -l app=todo-api -n todo-app --timeout=120s

# Ver el estado general

$ kubectl get all -n todo-app
NAME                            READY   STATUS    RESTARTS      AGE
pod/mysql-69dbdc49b-2tqln       1/1     Running   1 (17m ago)   24h
pod/todo-api-54bf9d95ff-l9qz9   1/1     Running   1 (17m ago)   24h
pod/todo-api-54bf9d95ff-m88rn   1/1     Running   1 (17m ago)   24h

NAME                   TYPE        CLUSTER-IP      EXTERNAL-IP   PORT(S)    AGE
service/mysql-svc      ClusterIP   10.101.146.41   <none>        3306/TCP   24h
service/todo-api-svc   ClusterIP   10.96.89.165    <none>        80/TCP     24h

NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
deployment.apps/mysql      1/1     1            1           24h
deployment.apps/todo-api   2/2     2            2           24h

NAME                                  DESIRED   CURRENT   READY   AGE
replicaset.apps/mysql-69dbdc49b       1         1         1       24h
replicaset.apps/todo-api-54bf9d95ff   2         2         2       24h


kubectl get gateway,httproute -n todo-app
```

Salida esperada:
```
NAME                                    CLASS   ADDRESS   PROGRAMMED   AGE
gateway.gateway.networking.k8s.io/todo-gateway   eg    1.2.3.4   True    30s

NAME                                          HOSTNAMES   AGE
httproute.gateway.networking.k8s.io/todo-route            30s
```

### Paso 7 — Obtener la IP del Gateway y acceder

A diferencia del Ingress, la IP la asigna el Gateway directamente:

```bash
# Obtener la IP asignada al Gateway
kubectl get gateway todo-gateway -n todo-app \
  -o jsonpath='{.status.addresses[0].value}'
```

Si en Minikube la IP no es accesible directamente (driver Docker en Linux), lanzar el tunnel:

```bash
# Terminal 1 — dejar corriendo
sudo -E minikube tunnel

# Terminal 2 — obtener IP del Gateway
GATEWAY_IP=$(kubectl get gateway todo-gateway -n todo-app \
  -o jsonpath='{.status.addresses[0].value}')

echo "Gateway IP: $GATEWAY_IP"

# Probar
curl http://$GATEWAY_IP/health
curl http://$GATEWAY_IP/todos
```

### Paso 8 — Probar los endpoints

```bash
GATEWAY_IP=$(kubectl get gateway todo-gateway -n todo-app \
  -o jsonpath='{.status.addresses[0].value}')

# Health check
curl -s http://$GATEWAY_IP/health | jq .

# Listar Todos
curl -s http://$GATEWAY_IP/todos | jq .

# Crear un Todo
curl -s -X POST http://$GATEWAY_IP/todos \
  -H "Content-Type: application/json" \
  -d '{"title": "Mi primer Todo con Gateway API"}' | jq .

# Swagger UI
open http://$GATEWAY_IP
```

---

## Diferencia clave en los manifiestos

### Antes (Ingress)

```yaml
# Un único recurso mezclaba listener y reglas de enrutamiento
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: todo-ingress
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /  # config vía anotaciones
spec:
  ingressClassName: nginx
  rules:
    - host: todo.local
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: todo-api-svc
                port:
                  number: 80
```

### Ahora (Gateway API)

```yaml
# Gateway — define el punto de entrada (responsabilidad de infraestructura)
apiVersion: gateway.networking.k8s.io/v1
kind: Gateway
metadata:
  name: todo-gateway
  namespace: todo-app
spec:
  gatewayClassName: eg
  listeners:
    - name: http
      port: 80
      protocol: HTTP
      allowedRoutes:
        namespaces:
          from: Same
---
# HTTPRoute — define las reglas (responsabilidad de la aplicación)
apiVersion: gateway.networking.k8s.io/v1
kind: HTTPRoute
metadata:
  name: todo-route
  namespace: todo-app
spec:
  parentRefs:
    - name: todo-gateway
  rules:
    - matches:
        - path:
            type: PathPrefix
            value: /
      backendRefs:
        - name: todo-api-svc
          port: 80
```

La configuración ya no depende de **anotaciones** específicas del controller (como `nginx.ingress.kubernetes.io/...`) sino de **campos tipados** del recurso, lo que hace el manifiesto más portable y legible.

---

## Comandos útiles

```bash
# Ver el estado del Gateway y si tiene IP asignada
kubectl describe gateway todo-gateway -n todo-app

# Ver las rutas configuradas en el HTTPRoute
kubectl describe httproute todo-route -n todo-app

# Ver logs de Envoy Gateway
kubectl logs -n envoy-gateway-system \
  -l app.kubernetes.io/name=gateway -f

# Ver logs de la API
kubectl logs -n todo-app -l app=todo-api -f

# Shell en un pod de la API
kubectl exec -it -n todo-app \
  $(kubectl get pod -n todo-app -l app=todo-api \
    -o jsonpath='{.items[0].metadata.name}') \
  -- /bin/sh

# Port-forward de emergencia si el Gateway no es alcanzable
kubectl port-forward svc/todo-api-svc 5000:80 -n todo-app
```

curl http://localhost:5000/health

---

## Limpiar el entorno

```bash
# Eliminar la aplicación
kubectl delete namespace todo-app

# Eliminar Envoy Gateway
kubectl delete -f https://github.com/envoyproxy/gateway/releases/download/v1.2.0/install.yaml

# Eliminar los CRDs de Gateway API
kubectl delete -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.2.0/standard-install.yaml

# Parar Minikube
minikube stop

# Eliminar el clúster
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
| `GET` | `/health` | — | Estado de la API y conexión MySQL |
